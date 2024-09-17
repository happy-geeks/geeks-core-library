using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Options;
using System;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.ItemFiles.Models;
using Constants = GeeksCoreLibrary.Modules.ItemFiles.Models.Constants;

namespace GeeksCoreLibrary.Modules.ItemFiles.Services
{
    public class CachedItemFilesService : IItemFilesService
    {
        private readonly GclSettings gclSettings;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IItemFilesService itemFilesService;

        public CachedItemFilesService(IOptions<GclSettings> gclSettings, IItemFilesService itemFilesService, IWebHostEnvironment webHostEnvironment = null)
        {
            this.gclSettings = gclSettings.Value;
            this.webHostEnvironment = webHostEnvironment;
            this.itemFilesService = itemFilesService;
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemImageAsync(ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
        {
            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localFilename = $"image_wiser2{entityTypePart}_{itemId}_{propertyName}_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{fileNumber}_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserItemImageAsync(itemId, propertyName, preferredWidth, preferredHeight, filename, fileNumber, resizeMode, anchorPosition, encryptedItemId, entityType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null, int linkType = 0)
        {
            var linkTypePart = linkType == 0 ? "" : $"_{linkType}";
            var localFilename = $"image_wiser2_{itemLinkId}_itemlink{linkTypePart}_{propertyName}_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{fileNumber}_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserItemLinkImageAsync(itemLinkId, propertyName, preferredWidth, preferredHeight, filename, fileNumber, resizeMode, anchorPosition, encryptedItemLinkId, linkType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectImageAsync(ulong itemId, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null)
        {
            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localFilename = $"image_wiser2{entityTypePart}_{itemId}_direct_{resizeMode:G}-{anchorPosition:G}_{preferredWidth}_{preferredHeight}_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserDirectImageAsync(itemId, preferredWidth, preferredHeight, filename, resizeMode, anchorPosition, encryptedItemId, entityType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemFileAsync(ulong itemId, string propertyName, string filename, int fileNumber, string encryptedItemId = null, string entityType = null)
        {
            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localFilename = $"file_wiser2{entityTypePart}_{itemId}_{propertyName}_{fileNumber}_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserItemFileAsync(itemId, propertyName, filename, fileNumber, encryptedItemId, entityType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string filename, int fileNumber, string encryptedItemLinkId = null, int linkType = 0)
        {
            var linkTypePart = linkType == 0 ? "" : $"_{linkType}";
            var localFilename = $"file_wiser2_{itemLinkId}_itemlink{linkTypePart}_{propertyName}_{fileNumber}_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserItemLinkFileAsync(itemLinkId, propertyName, filename, fileNumber, encryptedItemLinkId, linkType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <inheritdoc />
        public async Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectFileAsync(ulong itemId, string filename, string encryptedItemId = null, string entityType = null)
        {
            var entityTypePart = String.IsNullOrWhiteSpace(entityType) ? "" : $"_{entityType}";
            var localFilename = $"file_wiser2{entityTypePart}_{itemId}_direct_{filename}";
            if (gclSettings.DefaultItemFileCacheDuration.TotalSeconds <= 0 || !ValidateItemFile(localFilename))
            {
                return await itemFilesService.GetWiserDirectFileAsync(itemId, filename, encryptedItemId, entityType);
            }

            return await GetFileBytesAsync(localFilename);
        }

        /// <summary>
        /// Validates an image. This will check if the image exists, and if the cache time has not expired yet.
        /// </summary>
        /// <param name="localFilename"></param>
        /// <returns></returns>
        private bool ValidateItemFile(string localFilename)
        {
            if (webHostEnvironment == null)
            {
                return false;
            }

            var localDirectory = Path.Combine(webHostEnvironment.WebRootPath, Constants.DefaultFilesDirectory);
            if (!Directory.Exists(localDirectory))
            {
                return false;
            }

            var fileLocation = Path.Combine(localDirectory, localFilename);
            var fileInfo = new FileInfo(fileLocation);
            return fileInfo.Exists && DateTime.UtcNow.Subtract(fileInfo.LastWriteTimeUtc) <= gclSettings.DefaultItemFileCacheDuration;
        }

        /// <summary>
        /// Retrieves a file's bytes from the local content files folder.
        /// </summary>
        /// <param name="localFilename"></param>
        /// <returns></returns>
        private async Task<(byte[] FileBytes, DateTime LastModified)> GetFileBytesAsync(string localFilename)
        {
            if (webHostEnvironment == null)
            {
                return (null, DateTime.MinValue);
            }

            var localDirectory = Path.Combine(webHostEnvironment.WebRootPath, Constants.DefaultFilesDirectory);
            var fileLocation = Path.Combine(localDirectory, localFilename);

            var fileBytes = await File.ReadAllBytesAsync(fileLocation);
            var lastModified = File.GetLastWriteTimeUtc(fileLocation);

            return (fileBytes, lastModified);
        }
    }
}