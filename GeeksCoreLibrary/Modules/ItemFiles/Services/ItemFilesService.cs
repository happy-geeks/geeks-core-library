using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
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

namespace GeeksCoreLibrary.Modules.ItemFiles.Services
{
    public class ItemFilesService : IItemFilesService, IScopedService
    {
        private readonly ILogger<ItemFilesService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IWiserItemsService wiserItemsService;
        private readonly IHttpClientService httpClientService;

        public ItemFilesService(ILogger<ItemFilesService> logger,
            IDatabaseConnection databaseConnection,
            IObjectsService objectsService,
            IWiserItemsService wiserItemsService,
            IHttpClientService httpClientService,
            IHttpContextAccessor httpContextAccessor = null,
            IWebHostEnvironment webHostEnvironment = null)
        {
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.httpContextAccessor = httpContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.wiserItemsService = wiserItemsService;
            this.httpClientService = httpClientService;
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemImageAsync(ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
        {
            if (!TryGetFinalId(itemId, encryptedItemId, out var finalItemId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);
            if (fileNumber < 1)
            {
                fileNumber = 1;
            }

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("itemId", finalItemId);
            databaseConnection.AddParameter("propertyName", propertyName);
            var getImageResult = await databaseConnection.GetAsync($@"
                SELECT content_type, content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE item_id = ?itemId AND property_name = ?propertyName
                ORDER BY ordering ASC, id ASC
                LIMIT {fileNumber - 1},1");

            if (!ValidateQueryResult(getImageResult, encryptedItemId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve image because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"image_wiser2{entityTypePart}_{finalItemId}_{propertyName}_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{fileNumber}_{Path.GetFileName(filename)}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            // Calling HandleImage with the dataRow parameter set to null will cause the function to return a no-image if possible.
            return getImageResult.Rows.Count == 0
                ? await HandleImage(null, fileLocation, propertyName, preferredWidth, preferredHeight, resizeMode, anchorPosition)
                : await HandleImage(getImageResult.Rows[0], fileLocation, propertyName, preferredWidth, preferredHeight, resizeMode, anchorPosition);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null, int linkType = 0)
        {
            if (!TryGetFinalId(itemLinkId, encryptedItemLinkId, out var finalItemLinkId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("itemLinkId", finalItemLinkId);
            databaseConnection.AddParameter("propertyName", propertyName);
            var getImageResult = await databaseConnection.GetAsync($@"
                SELECT content_type, content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE itemlink_id = ?itemLinkId AND property_name = ?propertyName
                ORDER BY ordering ASC, id ASC
                LIMIT {fileNumber - 1},1");

            if (!ValidateQueryResult(getImageResult, encryptedItemLinkId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var linkTypePart = linkType == 0 ? "" : $"_{linkType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve image because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"image_wiser2_{finalItemLinkId}_itemlink{linkTypePart}_{propertyName}_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{fileNumber}_{filename}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            // Calling HandleImage with the dataRow parameter set to null will cause the function to return a no-image if possible.
            return getImageResult.Rows.Count == 0
                ? await HandleImage(null, fileLocation, propertyName, preferredWidth, preferredHeight, resizeMode, anchorPosition)
                : await HandleImage(getImageResult.Rows[0], fileLocation, propertyName, preferredWidth, preferredHeight, resizeMode, anchorPosition);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectImageAsync(ulong itemId, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
        {
            if (!TryGetFinalId(itemId, encryptedItemId, out var finalItemId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("fileId", finalItemId);
            var getImageResult = await databaseConnection.GetAsync($@"
                SELECT content_type, content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE id = ?fileId");

            if (!ValidateQueryResult(getImageResult, encryptedItemId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve image because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"image_wiser2{entityTypePart}_{finalItemId}_direct_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{Path.GetFileName(filename)}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            // Calling HandleImage with the dataRow parameter set to null will cause the function to return a no-image if possible.
            return getImageResult.Rows.Count == 0
                ? await HandleImage(null, fileLocation, "", preferredWidth, preferredHeight, resizeMode, anchorPosition)
                : await HandleImage(getImageResult.Rows[0], fileLocation, "", preferredWidth, preferredHeight, resizeMode, anchorPosition);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemFileAsync(ulong itemId, string propertyName, string filename, int fileNumber, string encryptedItemId = null, string entityType = null)
        {
            if (!TryGetFinalId(itemId, encryptedItemId, out var finalItemId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("itemId", finalItemId);
            databaseConnection.AddParameter("propertyName", propertyName);
            var getFileResult = await databaseConnection.GetAsync($@"
                SELECT content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE item_id = ?itemId AND property_name = ?propertyName
                ORDER BY ordering ASC, id ASC
                LIMIT {fileNumber - 1},1");

            if (!ValidateQueryResult(getFileResult, encryptedItemId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve file because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"file_wiser2{entityTypePart}_{finalItemId}_{propertyName}_{fileNumber}_{Path.GetFileName(filename)}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            return getFileResult.Rows.Count == 0
                ? (null, DateTime.MinValue)
                : await HandleFile(getFileResult.Rows[0], fileLocation);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string filename, int fileNumber, string encryptedItemLinkId = null, int linkType = 0)
        {
            if (!TryGetFinalId(itemLinkId, encryptedItemLinkId, out var finalItemLinkId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForLinkAsync(linkType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("itemLinkId", finalItemLinkId);
            databaseConnection.AddParameter("propertyName", propertyName);
            var getFileResult = await databaseConnection.GetAsync($@"
                SELECT content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE itemlink_id = ?itemLinkId AND property_name = ?propertyName
                ORDER BY ordering ASC, id ASC
                LIMIT {fileNumber - 1},1");

            if (!ValidateQueryResult(getFileResult, encryptedItemLinkId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var linkTypePart = linkType == 0 ? "" : $"_{linkType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve file because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"file_wiser2_{finalItemLinkId}_itemlink{linkTypePart}_{propertyName}_{fileNumber}_{Path.GetFileName(filename)}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            return getFileResult.Rows.Count == 0
                ? (null, DateTime.MinValue)
                : await HandleFile(getFileResult.Rows[0], fileLocation);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectFileAsync(ulong itemId, string filename, string encryptedItemId = null, string entityType = null)
        {
            if (!TryGetFinalId(itemId, encryptedItemId, out var finalItemId))
            {
                return (null, DateTime.MinValue);
            }

            var tablePrefix = await wiserItemsService.GetTablePrefixForEntityAsync(entityType);

            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("fileId", finalItemId);
            var getFileResult = await databaseConnection.GetAsync($@"
                SELECT content, content_url, protected
                FROM `{tablePrefix}{WiserTableNames.WiserItemFile}`
                WHERE id = ?fileId");

            if (!ValidateQueryResult(getFileResult, encryptedItemId))
            {
                // If file is protected, but tried to retrieve it without an encrypted item ID should result in a 404 status.
                return (null, DateTime.MinValue);
            }

            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localDirectory = FileSystemHelpers.GetContentFilesFolderPath(webHostEnvironment);
            if (String.IsNullOrWhiteSpace(localDirectory))
            {
                logger.LogError($"Could not retrieve file because the directory '{Models.Constants.DefaultFilesDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
                return (null, DateTime.MinValue);
            }

            var localFilename = $"file_wiser2{entityTypePart}_{finalItemId}_direct_{Path.GetFileName(filename)}";
            var fileLocation = Path.Combine(localDirectory, localFilename);

            return getFileResult.Rows.Count == 0
                ? (null, DateTime.MinValue)
                : await HandleFile(getFileResult.Rows[0], fileLocation);
        }

        /// <summary>
        /// Returns true if encryptedId was null or empty, or if the decryption succeeded.
        /// </summary>
        /// <param name="id"></param>
        /// <param name="encryptedId"></param>
        /// <param name="result">Either the result of encryptedId if it was a valid encrypted value, the value of <paramref name="id"/>, or 0 if <paramref name="encryptedId"/> contained an invalid value.</param>
        /// <returns></returns>
        private bool TryGetFinalId(ulong id, string encryptedId, out ulong result)
        {
            if (String.IsNullOrWhiteSpace(encryptedId))
            {
                result = id;
                return true;
            }

            try
            {
                var unencryptedId = encryptedId.DecryptWithAesWithSalt(withDateTime: true);
                return UInt64.TryParse(unencryptedId, out result);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to decrypt encrypted item ID when trying to retrieve image. Encrypted value might have expired, or encrypted using a different encryption key.");
                result = 0;
                return false;
            }
        }

        /// <summary>
        /// Validates the result of the query. Will return <see langword="false"/> if the resulting file is protected, but no encrypted ID was used.
        /// Will return <see langword="true"/> in all other cases, even if the query result was empty (no rows).
        /// </summary>
        /// <param name="queryResult"></param>
        /// <param name="encryptedId"></param>
        /// <returns></returns>
        private static bool ValidateQueryResult(DataTable queryResult, string encryptedId)
        {
            if (queryResult.Rows.Count == 0)
            {
                // No result should still return true, so a no-image can be used instead.
                return true;
            }

            var isProtected = Convert.ToBoolean(queryResult.Rows[0]["protected"]);
            return !isProtected || !String.IsNullOrWhiteSpace(encryptedId);
        }

        /// <summary>
        /// Convers the data into an image of the given size, format, and quality, and will return the bytes of that image.
        /// </summary>
        /// <param name="dataRow"></param>
        /// <param name="saveLocation"></param>
        /// <param name="propertyName"></param>
        /// <param name="preferredWidth"></param>
        /// <param name="preferredHeight"></param>
        /// <param name="resizeMode"></param>
        /// <param name="anchorPosition"></param>
        /// <returns></returns>
        private async Task<(byte[] FileBytes, DateTime LastModified)> HandleImage(DataRow dataRow, string saveLocation, string propertyName, int preferredWidth, int preferredHeight, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
        {
            if (String.IsNullOrEmpty(webHostEnvironment?.WebRootPath))
            {
                return (null, DateTime.MinValue);
            }

            byte[] fileBytes;
            var imageIsProtected = false;

            if (dataRow == null)
            {
                var extension = Path.GetExtension(saveLocation);

                // No row found in the database, check if a no-image file is available.
                var noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg_{propertyName}{extension}");
                if (!File.Exists(noImageFilePath))
                {
                    noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg{extension}");
                    if (!File.Exists(noImageFilePath))
                    {
                        // A no-image file could not be found; abort.
                        return (null, DateTime.MinValue);
                    }
                }

                // No-image file is available, use that the image.
                fileBytes = await File.ReadAllBytesAsync(noImageFilePath);
            }
            else
            {
                // Check if image is protected.
                imageIsProtected = Convert.ToBoolean(dataRow["protected"]);

                // Retrieve the image bytes from the data row.
                fileBytes = dataRow.Field<byte[]>("content");

                if (fileBytes == null || fileBytes.Length == 0)
                {
                    // Data row didn't contain a file directly, but might contain a content URL.
                    var contentUrl = dataRow.Field<string>("content_url");

                    if (!String.IsNullOrWhiteSpace(contentUrl))
                    {
                        var requestUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);

                        if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri) && contentUri.GetLeftPart(UriPartial.Authority).Equals(requestUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                        {
                            contentUrl = contentUri.LocalPath;
                        }

                        if (Uri.IsWellFormedUriString(contentUrl, UriKind.Absolute))
                        {
                            var requestUri = new Uri(contentUrl);
                            fileBytes = await httpClientService.Client.GetByteArrayAsync(requestUri);
                        }
                        else
                        {
                            var localFilePath = Path.Combine(webHostEnvironment.WebRootPath, contentUrl.TrimStart('/'));
                            if (File.Exists(localFilePath))
                            {
                                fileBytes = await File.ReadAllBytesAsync(localFilePath);
                            }
                        }
                    }
                }

                var extension = Path.GetExtension(saveLocation);

                // Final check to see if a the image bytes were retrieved.
                if (fileBytes == null || fileBytes.Length == 0)
                {
                    // Try to get a no-image file instead.
                    var noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg_{propertyName}{extension}");
                    if (!File.Exists(noImageFilePath))
                    {
                        noImageFilePath = Path.Combine(webHostEnvironment.WebRootPath, "img", $"noimg{extension}");
                        if (!File.Exists(noImageFilePath))
                        {
                            // A no-image file could not be found; abort.
                            return (null, DateTime.MinValue);
                        }
                    }

                    fileBytes = await File.ReadAllBytesAsync(noImageFilePath);
                }

                // Simply return the content without trying to alter it when it's an SVG.
                var contentType = dataRow.Field<string>("content_type") ?? "";
                if (contentType.Equals("image/svg+xml", StringComparison.OrdinalIgnoreCase))
                {
                    return (fileBytes, DateTime.UtcNow);
                }
            }

            var outFileBytes = await ResizeImageWithImageMagick(fileBytes, saveLocation, imageIsProtected, preferredWidth, preferredHeight, resizeMode, anchorPosition);

            return (outFileBytes, DateTime.UtcNow);
        }

        private async Task<(byte[] FileBytes, DateTime LastModified)> HandleFile(DataRow dataRow, string saveLocation)
        {
            if (dataRow == null)
            {
                return (null, DateTime.MinValue);
            }

            // Check if image is protected.
            var fileIsProtected = Convert.ToBoolean(dataRow["protected"]);

            // Retrieve the image bytes from the data row.
            var fileBytes = dataRow.Field<byte[]>("content");

            if (fileBytes == null || fileBytes.Length == 0)
            {
                // Data row didn't contain a file directly, but might contain a content URL.
                var contentUrl = dataRow.Field<string>("content_url");

                if (!String.IsNullOrWhiteSpace(contentUrl))
                {
                    var requestUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor?.HttpContext);

                    if (Uri.TryCreate(contentUrl, UriKind.Absolute, out var contentUri) && contentUri.GetLeftPart(UriPartial.Authority).Equals(requestUrl.GetLeftPart(UriPartial.Authority), StringComparison.OrdinalIgnoreCase))
                    {
                        contentUrl = contentUri.LocalPath;
                    }

                    if (Uri.IsWellFormedUriString(contentUrl, UriKind.Absolute))
                    {
                        var requestUri = new Uri(contentUrl);
                        fileBytes = await httpClientService.Client.GetByteArrayAsync(requestUri);
                    }
                    else
                    {
                        var localFilePath = Path.Combine(webHostEnvironment.WebRootPath, contentUrl.TrimStart('/'));
                        if (File.Exists(localFilePath))
                        {
                            fileBytes = await File.ReadAllBytesAsync(localFilePath);
                        }
                    }
                }
            }

            // Final check to see if a the image bytes were retrieved.
            if (fileBytes == null || fileBytes.Length == 0)
            {
                return (null, DateTime.MinValue);
            }

            // Don't save the file to the disk if it's protected (protected files shouldn't be cached).
            if (!fileIsProtected)
            {
                await File.WriteAllBytesAsync(saveLocation, fileBytes);
            }

            return (fileBytes, DateTime.UtcNow);
        }

        /// <summary>
        /// Internal function to resize the image using the Magick.NET library. The extension of <paramref name="saveLocation"/> will be used to determine the image's file format.
        /// </summary>
        /// <param name="fileBytes">The byte array of the source image.</param>
        /// <param name="saveLocation">The location on the file system where the result should be saved. Note that the image will not be saved to the disk if <paramref name="imageIsProtected"/> is set to <see langword="true"/>.</param>
        /// <param name="imageIsProtected">Whether the image that is being resized is protected. If set to <see langword="true"/>, it will not be saved to disk.</param>
        /// <param name="preferredWidth">The width the resized image should preferably be resized to. Depending on the resize mod and dimensions of the source file the resized image might not be resized to the preferred width.</param>
        /// <param name="preferredHeight">The height the resized image should preferably be resized to. Depending on the resize mod and dimensions of the source file the resized image might not be resized to the preferred height.</param>
        /// <param name="resizeMode">The method of resizing.</param>
        /// <param name="anchorPosition">The anchor position that the <see cref="ResizeModes.Crop"/> and <see cref="ResizeModes.Fill"/> resize modes use.</param>
        /// <returns>The byte array of the resized image.</returns>
        private async Task<byte[]> ResizeImageWithImageMagick(byte[] fileBytes, string saveLocation, bool imageIsProtected, int preferredWidth, int preferredHeight, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
        {
            var extension = Path.GetExtension(saveLocation);

            // Determine image format.
            MagickFormat imageFormat;
            var imageQuality = 100;
            switch (extension.ToLowerInvariant())
            {
                case ".jpg":
                case ".jpeg":
                    imageFormat = MagickFormat.Jpg;
                    imageQuality = 75;
                    break;
                case ".gif":
                    imageFormat = MagickFormat.Gif;
                    break;
                case ".png":
                    imageFormat = MagickFormat.Png;
                    break;
                case ".webp":
                    imageFormat = MagickFormat.WebP;
                    if (!Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("image_webp_quality", "80"), out imageQuality))
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
                default:
                    throw new NotSupportedException("Unsupported file type.");
            }

            byte[] outFileBytes;
            if (preferredWidth > 0 && preferredHeight > 0)
            {
                var fillColor = MagickColors.Transparent;
                if (!extension.InList(".gif", ".png", ".webp"))
                {
                    fillColor = MagickColors.White;
                }

                // GIF images are a bit different because they have multiple frames.
                if (imageFormat == MagickFormat.Gif)
                {
                    using var collection = new MagickImageCollection(fileBytes);

                    // This will remove the optimization and change the image to how it looks at that point
                    // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                    collection.Coalesce();

                    foreach (var frame in collection)
                    {
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
                                throw new ArgumentOutOfRangeException(nameof(resizeMode), "Unknown resize mode.");
                        }
                    }

                    collection.OptimizePlus();
                    collection.OptimizeTransparency();

                    outFileBytes = collection.ToByteArray();
                }
                else
                {
                    using var image = new MagickImage(fileBytes);

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
                            throw new ArgumentOutOfRangeException(nameof(resizeMode), "Unknown resize mode.");
                    }

                    outFileBytes = image.ToByteArray(imageFormat);
                }
            }
            else
            {
                if (imageFormat == MagickFormat.Gif)
                {
                    using var collection = new MagickImageCollection(fileBytes);

                    // This will remove the optimization and change the image to how it looks at that point
                    // during the animation. More info here: http://www.imagemagick.org/Usage/anim_basics/#coalesce
                    collection.Coalesce();

                    // Now, after removing the original optimizations, optimize the result again.
                    // This can potentially reduce the file size by quite a bit.
                    collection.OptimizePlus();
                    collection.OptimizeTransparency();

                    outFileBytes = collection.ToByteArray();
                }
                else
                {
                    using var image = new MagickImage(fileBytes);

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

                    outFileBytes = image.ToByteArray(imageFormat);
                }
            }

            // Save file to disk if it isn't protected.
            if (!imageIsProtected)
            {
                await using var outFileStream = new FileStream(saveLocation, FileMode.Create, FileAccess.Write);
                outFileStream.Write(outFileBytes, 0, outFileBytes.Length);
            }

            return outFileBytes;
        }
    }
}