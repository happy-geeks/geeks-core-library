using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mime;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Amazon.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using GeeksCoreLibrary.Modules.ItemFiles.Helpers;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using ImageMagick;
using ImageMagick.Formats;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Modules.ItemFiles.Services;

/// <inheritdoc cref="IItemFilesService" />
public class ItemFilesService(
    ILogger<ItemFilesService> logger,
    IDatabaseConnection databaseConnection,
    IFileCacheService fileCacheService,
    IObjectsService objectsService,
    IWiserItemsService wiserItemsService,
    IHttpClientService httpClientService,
    IAmazonS3Service amazonS3Service,
    IHttpContextAccessor httpContextAccessor = null,
    IWebHostEnvironment webHostEnvironment = null)
    : IItemFilesService, IScopedService
{
    /// <inheritdoc />
    public async Task<WiserItemFileModel> GetFileAsync(FileLookupTypes lookupType, object id, string propertyName = null, string fileName = null, string entityType = null, int linkType = 0, int fileNumber = 1, bool includeContent = true)
    {
        // Property name needs to have a value, except when getting a file via it's own ID.
        if (String.IsNullOrWhiteSpace(propertyName) && lookupType is not FileLookupTypes.ItemFileId and not FileLookupTypes.ItemLinkFileId)
        {
            throw new ArgumentNullException(nameof(propertyName));
        }

        // If we are looking up by file name, the file name must be provided.
        if (lookupType is FileLookupTypes.ItemLinkFileName or FileLookupTypes.ItemFileName && String.IsNullOrWhiteSpace(fileName))
        {
            throw new ArgumentNullException(nameof(fileName));
        }

        // Parse the ID and decrypt it if it's a string.
        var parsedId = 0UL;
        var hasValidEncryptedId = false;
        switch (id)
        {
            case ulong ulongId:
                parsedId = ulongId;
                break;
            case string encryptedId:
                try
                {
                    var unencryptedId = encryptedId.DecryptWithAesWithSalt(withDateTime: true);
                    parsedId = UInt64.Parse(unencryptedId);
                    hasValidEncryptedId = true;
                }
                catch (Exception exception)
                {
                    logger.LogError(exception, "Failed to decrypt encrypted ID when trying to retrieve a file. Encrypted value might have expired, or encrypted using a different encryption key.");
                }

                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(id), "ID must be either an ulong or a string.");
        }

        // We need a valid ID to continue.
        if (parsedId == 0)
        {
            throw new ArgumentNullException(nameof(id));
        }

        // The file number should never be lower than 1.
        if (fileNumber < 1)
        {
            fileNumber = 1;
        }

        // Generate the query to get the file.
        var whereClause = new List<string>();
        string tablePrefix;
        switch (lookupType)
        {
            case FileLookupTypes.ItemId:
            case FileLookupTypes.ItemFileName:
                tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
                whereClause.Add("item_id = ?id");
                whereClause.Add("property_name = ?propertyName");
                break;
            case FileLookupTypes.ItemFileId:
                tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
                whereClause.Add("id = ?id");
                break;
            case FileLookupTypes.ItemLinkId:
            case FileLookupTypes.ItemLinkFileName:
                tablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType, entityType);
                whereClause.Add("itemlink_id = ?id");
                whereClause.Add("property_name = ?propertyName");
                break;
            case FileLookupTypes.ItemLinkFileId:
                tablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType, entityType);
                whereClause.Add("id = ?id");
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(lookupType), lookupType, null);
        }

        databaseConnection.AddParameter("id", parsedId);
        databaseConnection.AddParameter("propertyName", propertyName);
        databaseConnection.AddParameter("fileNumber", fileNumber);

        var columnsToGet = new List<string> {"id", "item_id", "content_type", "file_name", "extension", "added_on", "added_by", "property_name", "protected", "itemlink_id"};
        if (includeContent)
        {
            columnsToGet.AddRange(["content", "content_url", "extra_data", "title"]);
        }

        var query = $"""
                     SELECT {String.Join(", ", columnsToGet)}
                     FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                     WHERE {String.Join(" AND ", whereClause)}
                     ORDER BY ordering ASC, id ASC
                     """;

        var dataTable = await databaseConnection.GetAsync(query, skipCache: true);

        // If the file was not found, return null.
        if (dataTable.Rows.Count == 0)
        {
            return null;
        }

        WiserItemFileModel result = null;
        if (lookupType is FileLookupTypes.ItemFileId or FileLookupTypes.ItemLinkFileId)
        {
            // If we are looking up by file ID, we can just return the first row.
            // Use Single LinQ method, so that we get an error if there is less or more than one row.
            result = WiserFileHelpers.DataRowToItemFile(dataTable.Rows.Cast<DataRow>().Single());
        }
        else
        {
            // For other loookup types, we need to loop through the rows to find the correct file.
            for (var index = 0; index < dataTable.Rows.Count; index++)
            {
                var dataRow = dataTable.Rows[index];
                var file = WiserFileHelpers.DataRowToItemFile(dataRow);

                // If the lookup type is by file name, we need to find the file with the correct name.
                if (lookupType is FileLookupTypes.ItemFileName or FileLookupTypes.ItemLinkFileName && String.Equals(Path.GetFileNameWithoutExtension(file.FileName), Path.GetFileNameWithoutExtension(fileName), StringComparison.OrdinalIgnoreCase))
                {
                    result = file;
                    break;
                }

                // If the lookup type is by file number, we need to find the file with the correct number.
                if (index + 1 != fileNumber)
                {
                    continue;
                }

                result = file;
                break;
            }
        }


        // If the file is protected, but no encrypted ID was used, return null.
        return result == null || (result.Protected && !hasValidEncryptedId) ? null : result;
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetResizedImageAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1, uint preferredWidth = 0, uint preferredHeight = 0, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
    {
        var file = await GetFileAsync(lookupType, id, propertyName: propertyName, fileName: fileName, entityType, linkType, fileNumber) ?? new WiserItemFileModel {Id = 0, PropertyName = propertyName ?? "Unknown", FileName = "Unknown.png"};

        if (!String.IsNullOrWhiteSpace(fileName))
        {
            file.FileName = fileName;
        }

        // If the file is a link to an external file, this will attempt to download the file and return the bytes.
        // This will also resize the image to the requested dimensions and convert it to a different format if needed.
        return await HandleImageAsync(file, preferredWidth, preferredHeight, resizeMode, anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserItemImageAsync(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
    {
        return await GetResizedImageAsync(FileLookupTypes.ItemId, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, propertyName: propertyName, entityType: entityType, fileNumber: fileNumber, preferredWidth: preferredWidth, preferredHeight: preferredHeight, resizeMode: resizeMode, anchorPosition: anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null, int linkType = 0)
    {
        return await GetResizedImageAsync(FileLookupTypes.ItemLinkId, String.IsNullOrWhiteSpace(encryptedItemLinkId) ? itemLinkId : encryptedItemLinkId, fileName: fileName, propertyName: propertyName, linkType: linkType, fileNumber: fileNumber, preferredWidth: preferredWidth, preferredHeight: preferredHeight, resizeMode: resizeMode, anchorPosition: anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserDirectImageAsync(ulong itemId, uint preferredWidth, uint preferredHeight, string fileName, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
    {
        return await GetResizedImageAsync(FileLookupTypes.ItemFileId, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, entityType: entityType, preferredWidth: preferredWidth, preferredHeight: preferredHeight, resizeMode: resizeMode, anchorPosition: anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserImageByFileNameAsync(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
    {
        return await GetResizedImageAsync(FileLookupTypes.ItemFileName, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, entityType: entityType, preferredWidth: preferredWidth, preferredHeight: preferredHeight, resizeMode: resizeMode, anchorPosition: anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetParsedFileAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1)
    {
        var file = await GetFileAsync(lookupType, id, propertyName: propertyName, fileName: fileName, entityType, linkType, fileNumber) ?? new WiserItemFileModel {Id = 0, PropertyName = propertyName ?? "Unknown", FileName = "Unknown.pdf"};

        if (!String.IsNullOrWhiteSpace(fileName))
        {
            file.FileName = fileName;
        }

        // If the file is a link to an external file, this will attempt to download the file and return the bytes.
        return await HandleFileAsync(file);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserItemFileAsync(ulong itemId, string propertyName, string fileName, int fileNumber, string encryptedItemId = null, string entityType = null)
    {
        return await GetParsedFileAsync(FileLookupTypes.ItemId, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, entityType: entityType);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string fileName, int fileNumber, string encryptedItemLinkId = null, int linkType = 0)
    {
        return await GetParsedFileAsync(FileLookupTypes.ItemLinkId, String.IsNullOrWhiteSpace(encryptedItemLinkId) ? itemLinkId : encryptedItemLinkId, fileName: fileName, linkType: linkType);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetWiserDirectFileAsync(ulong itemId, string fileName, string encryptedItemId = null, string entityType = null)
    {
        return await GetParsedFileAsync(FileLookupTypes.ItemFileId, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, entityType: entityType);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> HandleImageAsync(WiserItemFileModel file, uint preferredWidth, uint preferredHeight, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
    {
        var result = new FileResultModel {FileBytes = null, LastModified = DateTime.MinValue, WiserItemFile = file};
        if (String.IsNullOrEmpty(webHostEnvironment?.WebRootPath))
        {
            return result;
        }

        var extension = Path.GetExtension(file.FileName);

        if (file.Id == 0)
        {
            // No row found in the database, check if a no-image file is available.
            var noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg_{file.PropertyName}{extension}");
            if (!File.Exists(noImageFilePath))
            {
                noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg{extension}");
                if (!File.Exists(noImageFilePath))
                {
                    // A no-image file could not be found; abort.
                    return result;
                }
            }

            // No-image file is available, use that the image.
            result.FileBytes = await fileCacheService.GetBytesAsync(noImageFilePath);
        }
        else
        {
            // Retrieve the image bytes from the data row.
            result.FileBytes = file.Content;

            if (result.FileBytes == null || result.FileBytes.Length == 0)
            {
                // Data row didn't contain a file directly, but might contain a content URL.
                var contentUrl = file.ContentUrl;

                if (!String.IsNullOrWhiteSpace(contentUrl))
                {
                    if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri))
                    {
                        // Amazon S3 URLs are handled differently.
                        if (contentUri.Scheme.Equals("s3", StringComparison.OrdinalIgnoreCase))
                        {
                            var s3Bucket = contentUri.Host;
                            var s3Object = contentUri.LocalPath.TrimStart('/');

                            var localPath = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
                            if (await amazonS3Service.DownloadObjectFromBucketAsync(s3Bucket, s3Object, localPath))
                            {
                                result.FileBytes = await fileCacheService.GetBytesAsync(Path.Combine(localPath, s3Object));
                            }
                        }
                        else
                        {
                            // Check if the content URL is on the same domain as the request. If so, use the local path instead.
                            var requestUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);
                            if (contentUri.GetLeftPart(UriPartial.Authority).Equals(requestUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                            {
                                contentUrl = contentUri.LocalPath;
                            }
                        }
                    }

                    // No image bytes were found, try to retrieve the image from the content URL.
                    if (result.FileBytes == null || result.FileBytes.Length == 0)
                    {
                        if (Uri.IsWellFormedUriString(contentUrl, UriKind.Absolute))
                        {
                            var fileResult = await httpClientService.Client.GetAsync(contentUrl);
                            if (fileResult.StatusCode == HttpStatusCode.OK)
                            {
                                result.FileBytes = await fileResult.Content.ReadAsByteArrayAsync();
                            }
                        }
                        else
                        {
                            var localFilePath = Path.Combine(webHostEnvironment.WebRootPath, contentUrl.TrimStart('/'));
                            if (File.Exists(localFilePath))
                            {
                                result.FileBytes = await fileCacheService.GetBytesAsync(localFilePath);
                            }
                        }
                    }
                }
            }

            // Final check to see if the image bytes were retrieved.
            if (result.FileBytes == null || result.FileBytes.Length == 0)
            {
                // Try to get a no-image file instead.
                var noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg_{file.PropertyName}{extension}");
                if (!File.Exists(noImageFilePath))
                {
                    noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg{extension}");
                    if (!File.Exists(noImageFilePath))
                    {
                        // A no-image file could not be found; abort.
                        return result;
                    }
                }

                result.FileBytes = await fileCacheService.GetBytesAsync(noImageFilePath);
            }

            result.LastModified = DateTime.UtcNow;

            // SVG files are vector images, so there is no point in trying to resize them. Return it as is.
            var contentType = file.ContentType ?? "";
            if (contentType.Equals(MediaTypeNames.Image.Svg, StringComparison.OrdinalIgnoreCase))
            {
                return result;
            }
        }

        await ResizeImageWithImageMagickAsync(result, preferredWidth, preferredHeight, resizeMode, anchorPosition);

        return result;
    }

    /// <inheritdoc />
    public async Task<FileResultModel> HandleFileAsync(WiserItemFileModel file)
    {
        var result = new FileResultModel {FileBytes = null, LastModified = DateTime.MinValue, WiserItemFile = file};
        if (file == null || file.Id == 0)
        {
            return result;
        }

        if (result.FileBytes == null || result.FileBytes.Length == 0)
        {
            // Data row didn't contain a file directly, but might contain a content URL.
            var contentUrl = file.ContentUrl;

            if (!String.IsNullOrWhiteSpace(contentUrl))
            {
                var requestUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);

                if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri) && contentUri.GetLeftPart(UriPartial.Authority).Equals(requestUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                {
                    contentUrl = contentUri.LocalPath;
                }

                if (Uri.IsWellFormedUriString(contentUrl, UriKind.Absolute))
                {
                    var fileResult = await httpClientService.Client.GetAsync(contentUrl);
                    if (fileResult.StatusCode == HttpStatusCode.OK)
                    {
                        result.FileBytes = await fileResult.Content.ReadAsByteArrayAsync();
                    }
                }
                else
                {
                    var localFilePath = Path.Combine(webHostEnvironment.WebRootPath, contentUrl.TrimStart('/'));
                    if (File.Exists(localFilePath))
                    {
                        result.FileBytes = await File.ReadAllBytesAsync(localFilePath);
                    }
                }
            }
        }

        // Final check to see if the file bytes were retrieved.
        if (result.FileBytes is {Length: > 0})
        {
            result.LastModified = DateTime.UtcNow;
        }

        return result;
    }

    /// <summary>
    /// Internal function to resize the image using the Magick.NET library. The file extension from the <see cref="WiserItemFileModel.FileName"/> property will be used to determine the image's file format.
    /// </summary>
    /// <param name="file">The image data.</param>
    /// <param name="preferredWidth">The width the resized image should preferably be resized to. Depending on the resize mod and dimensions of the source file the resized image might not be resized to the preferred width.</param>
    /// <param name="preferredHeight">The height the resized image should preferably be resized to. Depending on the resize mod and dimensions of the source file the resized image might not be resized to the preferred height.</param>
    /// <param name="resizeMode">The method of resizing.</param>
    /// <param name="anchorPosition">The anchor position that the <see cref="ResizeModes.Crop"/> and <see cref="ResizeModes.Fill"/> resize modes use.</param>
    /// <returns>The byte array of the resized image.</returns>
    private async Task ResizeImageWithImageMagickAsync(FileResultModel file, uint preferredWidth, uint preferredHeight, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
    {
        var extension = Path.GetExtension(file.WiserItemFile.FileName);

        // Determine image format.
        MagickFormat imageFormat;
        var imageQuality = 100u;
        switch (extension?.ToLowerInvariant())
        {
            case ".jpg":
            case ".jpeg":
                imageFormat = MagickFormat.Jpg;
                imageQuality = 75;
                break;
            case ".jxl":
                imageFormat = MagickFormat.Jxl;
                break;
            case ".gif":
                imageFormat = MagickFormat.Gif;
                break;
            case ".png":
                imageFormat = MagickFormat.Png;
                break;
            case ".webp":
                imageFormat = MagickFormat.WebP;
                if (!UInt32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("image_webp_quality", "80"), out imageQuality))
                {
                    imageQuality = 80;
                }

                break;
            case ".tif":
                imageFormat = MagickFormat.Tif;
                break;
            case ".tiff":
                imageFormat = MagickFormat.Tiff;
                break;
            case ".avif":
            case ".avifs":
                imageFormat = MagickFormat.Avif;
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(extension), extension, null);
        }

        var fillColor = MagickColors.Transparent;
        if (!imageFormat.InList(MagickFormat.Gif, MagickFormat.Png, MagickFormat.WebP, MagickFormat.Tif, MagickFormat.Tiff, MagickFormat.Avif, MagickFormat.Jxl))
        {
            fillColor = MagickColors.White;
        }

        if (preferredWidth > 0 && preferredHeight > 0)
        {
            // GIF images are a bit different because they have multiple frames.
            if (imageFormat == MagickFormat.Gif)
            {
                using var collection = new MagickImageCollection(file.FileBytes);

                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                foreach (var frame in collection)
                {
                    if (fillColor != MagickColors.Transparent)
                    {
                        frame.BackgroundColor = fillColor;
                        frame.Alpha(AlphaOption.Remove);
                    }

                    switch (resizeMode)
                    {
                        case ResizeModes.Normal:
                            ResizeHelpers.Normal(frame, preferredWidth, preferredHeight);
                            break;
                        case ResizeModes.Stretch:
                            ResizeHelpers.Stretch(frame, preferredWidth, preferredHeight);
                            break;
                        case ResizeModes.Crop:
                            ResizeHelpers.Crop(frame, preferredWidth, preferredHeight, anchorPosition);
                            break;
                        case ResizeModes.Fill:
                            ResizeHelpers.Fill(frame, preferredWidth, preferredHeight, anchorPosition, fillColor);
                            break;
                        default:
                            throw new ArgumentOutOfRangeException(nameof(resizeMode), resizeMode, null);
                    }
                }

                collection.OptimizePlus();
                collection.OptimizeTransparency();

                file.FileBytes = collection.ToByteArray();
            }
            else
            {
                using var image = new MagickImage(file.FileBytes);

                if (fillColor != MagickColors.Transparent)
                {
                    image.BackgroundColor = fillColor;
                    image.Alpha(AlphaOption.Remove);
                }

                if (imageFormat.InList(MagickFormat.Jpg, MagickFormat.WebP))
                {
                    image.Quality = imageQuality;
                    if (imageFormat == MagickFormat.WebP)
                    {
                        image.Settings.SetDefines(new WebPWriteDefines
                        {
                            Lossless = imageQuality == 100
                        });
                    }
                }

                switch (resizeMode)
                {
                    case ResizeModes.Normal:
                        ResizeHelpers.Normal(image, preferredWidth, preferredHeight);
                        break;
                    case ResizeModes.Stretch:
                        ResizeHelpers.Stretch(image, preferredWidth, preferredHeight);
                        break;
                    case ResizeModes.Crop:
                        ResizeHelpers.Crop(image, preferredWidth, preferredHeight, anchorPosition);
                        break;
                    case ResizeModes.Fill:
                        ResizeHelpers.Fill(image, preferredWidth, preferredHeight, anchorPosition, fillColor);
                        break;
                    default:
                        throw new ArgumentOutOfRangeException(nameof(resizeMode), resizeMode, null);
                }

                file.FileBytes = image.ToByteArray(imageFormat);
            }
        }
        else
        {
            if (imageFormat == MagickFormat.Gif)
            {
                using var collection = new MagickImageCollection(file.FileBytes);

                // This will remove the optimization and change the image to how it looks at that point
                // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                collection.Coalesce();

                if (fillColor != MagickColors.Transparent)
                {
                    foreach (var frame in collection)
                    {
                        frame.BackgroundColor = fillColor;
                        frame.Alpha(AlphaOption.Remove);
                    }
                }

                // Now, after removing the original optimizations, optimize the result again.
                // This can potentially reduce the file size by quite a bit.
                collection.OptimizePlus();
                collection.OptimizeTransparency();

                file.FileBytes = collection.ToByteArray();
            }
            else
            {
                using var image = new MagickImage(file.FileBytes);

                if (fillColor != MagickColors.Transparent)
                {
                    image.BackgroundColor = fillColor;
                    image.Alpha(AlphaOption.Remove);
                }

                if (imageFormat.InList(MagickFormat.Jpg, MagickFormat.WebP))
                {
                    image.Quality = imageQuality;
                    if (imageFormat == MagickFormat.WebP)
                    {
                        image.Settings.SetDefines(new WebPWriteDefines
                        {
                            Lossless = imageQuality == 100
                        });
                    }
                }

                file.FileBytes = image.ToByteArray(imageFormat);
            }
        }
    }
}