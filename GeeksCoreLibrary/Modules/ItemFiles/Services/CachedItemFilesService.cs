using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Branches.Interfaces;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using LazyCache;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.ItemFiles.Services;

public class CachedItemFilesService(
    IOptions<GclSettings> gclSettings,
    IItemFilesService innerItemFilesService,
    IBranchesService branchesService,
    IAppCache cache,
    ICacheService cacheService,
    ILogger<CachedItemFilesService> logger,
    IFileCacheService fileCacheService,
    IWebHostEnvironment webHostEnvironment = null)
    : IItemFilesService
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    /// <inheritdoc />
    public async Task<WiserItemFileModel> GetFileAsync(FileLookupTypes lookupType, object id, string propertyName = null, string fileName = null, string entityType = null, int linkType = 0, int fileNumber = 1, bool includeContent = true)
    {
        var cacheName = $"GetFile_{id}_{propertyName}_{fileName}_{entityType}_{linkType}_{fileNumber}_{includeContent}_{branchesService.GetDatabaseNameFromCookie()}";
        return await cache.GetOrAddAsync(cacheName,
            async cacheEntry =>
            {
                cacheEntry.AbsoluteExpirationRelativeToNow = gclSettings.DefaultItemFileCacheDuration;
                return await innerItemFilesService.GetFileAsync(lookupType, id, propertyName, fileName, entityType, linkType, fileNumber, includeContent);
            }, cacheService.CreateMemoryCacheEntryOptions(CacheAreas.Files));
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetResizedImageAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1, uint preferredWidth = 0, uint preferredHeight = 0, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center)
    {
        // Get the file metadata, so we can check if and how we need to cache this file.
        var file = await GetFileAsync(lookupType, id, propertyName: propertyName, fileName: fileName, entityType, linkType, fileNumber, false) ?? new WiserItemFileModel {Id = 0, PropertyName = propertyName ?? "Unknown", FileName = "Unknown.png"};
        if (!String.IsNullOrWhiteSpace(fileName))
        {
            file.FileName = fileName;
        }

        // Don't cache the file if it's protected, because that means it can contain sensitive information.
        if (file.Protected)
        {
            return await innerItemFilesService.GetResizedImageAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber, preferredWidth, preferredHeight, resizeMode, anchorPosition);
        }

        var cacheDirectory = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(cacheDirectory))
        {
            logger.LogError($"Could not cache the file because the directory '{cacheDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
            return await innerItemFilesService.GetResizedImageAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber, preferredWidth, preferredHeight, resizeMode, anchorPosition);
        }

        // Generate the file name for caching.
        var fileNameParts = new List<string> { "wiser_image" };
        if (!String.IsNullOrWhiteSpace(entityType))
        {
            fileNameParts.Add(entityType);
        }
        if (linkType > 0)
        {
            fileNameParts.Add(linkType.ToString());
        }
        fileNameParts.Add(file.Id.ToString());
        fileNameParts.Add(resizeMode.ToString("G"));
        fileNameParts.Add(anchorPosition.ToString("G"));
        fileNameParts.Add(preferredWidth.ToString());
        fileNameParts.Add(preferredHeight.ToString());

        var fileLocation = Path.Combine(cacheDirectory, String.Join("_", fileNameParts));

        var (fileBytes, lastModifiedDate) = await fileCacheService.GetOrAddAsync(fileLocation, async () =>
        {
            var fileResult = await innerItemFilesService.GetResizedImageAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber, preferredWidth, preferredHeight, resizeMode, anchorPosition);
            return (fileResult.FileBytes, true);
        }, gclSettings.DefaultItemFileCacheDuration);

        return new FileResultModel
        {
            FileBytes = fileBytes,
            LastModified = lastModifiedDate,
            WiserItemFile = file
        };
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
        return await GetResizedImageAsync(FileLookupTypes.ItemFileName, String.IsNullOrWhiteSpace(encryptedItemId) ? itemId : encryptedItemId, fileName: fileName, propertyName: propertyName, entityType: entityType, preferredWidth: preferredWidth, preferredHeight: preferredHeight, resizeMode: resizeMode, anchorPosition: anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> GetParsedFileAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1)
    {
        // Get the file metadata, so we can check if and how we need to cache this file.
        var file = await GetFileAsync(lookupType, id, propertyName: propertyName, fileName: fileName, entityType, linkType, fileNumber, false) ?? new WiserItemFileModel {Id = 0, PropertyName = propertyName ?? "Unknown", FileName = "Unknown.pdf"};
        if (!String.IsNullOrWhiteSpace(fileName))
        {
            file.FileName = fileName;
        }

        // Don't cache the file if it's protected, because that means it can contain sensitive information.
        if (file.Protected)
        {
            return await innerItemFilesService.GetParsedFileAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber);
        }

        var cacheDirectory = FileSystemHelpers.GetFileCacheDirectory(webHostEnvironment);
        if (String.IsNullOrWhiteSpace(cacheDirectory))
        {
            logger.LogError($"Could not cache the file because the directory '{cacheDirectory}' does not exist. Please create it and give it modify permissions to the user that is running the website.");
            return await innerItemFilesService.GetParsedFileAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber);
        }

        // Generate the file name for caching.
        var fileNameParts = new List<string> { "wiser_file" };
        if (!String.IsNullOrWhiteSpace(entityType))
        {
            fileNameParts.Add(entityType);
        }
        if (linkType > 0)
        {
            fileNameParts.Add(linkType.ToString());
        }
        fileNameParts.Add(file.Id.ToString());
        fileNameParts.Add(fileNumber.ToString());

        var fileLocation = Path.Combine(cacheDirectory, String.Join("_", fileNameParts));

        var (fileBytes, lastModifiedDate) = await fileCacheService.GetOrAddAsync(fileLocation, async () =>
        {
            var fileResult = await innerItemFilesService.GetParsedFileAsync(lookupType, id, fileName, propertyName, entityType, linkType, fileNumber);
            return (fileResult.FileBytes, true);
        }, gclSettings.DefaultItemFileCacheDuration);

        return new FileResultModel
        {
            FileBytes = fileBytes,
            LastModified = lastModifiedDate,
            WiserItemFile = file
        };
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
        return await innerItemFilesService.HandleImageAsync(file, preferredWidth, preferredHeight, resizeMode, anchorPosition);
    }

    /// <inheritdoc />
    public async Task<FileResultModel> HandleFileAsync(WiserItemFileModel file)
    {
        return await innerItemFilesService.HandleFileAsync(file);
    }
}