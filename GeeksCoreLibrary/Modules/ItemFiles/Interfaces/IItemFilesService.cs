using System;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.ItemFiles.Interfaces
{
    public interface IItemFilesService
    {
        /// <summary>
        /// Attempts to retrieve an image linked to an item. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="propertyName"></param>
        /// <param name="preferredWidth"></param>
        /// <param name="preferredHeight"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="resizeMode"></param>
        /// <param name="anchorPosition"></param>
        /// <param name="encryptedItemId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemImageAsync(ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null);

        /// <summary>
        /// Attempts to retrieve an image linked to a link between two items. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemLinkId"></param>
        /// <param name="propertyName"></param>
        /// <param name="preferredWidth"></param>
        /// <param name="preferredHeight"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="resizeMode"></param>
        /// <param name="anchorPosition"></param>
        /// <param name="encryptedItemLinkId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null);

        /// <summary>
        /// Attempts to retrieve an image directly, via file ID. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="preferredWidth"></param>
        /// <param name="preferredHeight"></param>
        /// <param name="filename"></param>
        /// <param name="resizeMode"></param>
        /// <param name="anchorPosition"></param>
        /// <param name="encryptedItemId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectImageAsync(ulong itemId, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null);

        /// <summary>
        /// Attempts to retrieve a file linked to an item. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="propertyName"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="encryptedItemId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemFileAsync(ulong itemId, string propertyName, string filename, int fileNumber, string encryptedItemId = null);

        /// <summary>
        /// Attempts to retrieve a file linked to a link between two items. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemLinkId"></param>
        /// <param name="propertyName"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="encryptedItemLinkId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string filename, int fileNumber, string encryptedItemLinkId = null);

        /// <summary>
        /// Attempts to retrieve a file directly, via file ID. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="filename"></param>
        /// <param name="encryptedItemId"></param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectFileAsync(ulong itemId, string filename, string encryptedItemId = null);
    }
}
