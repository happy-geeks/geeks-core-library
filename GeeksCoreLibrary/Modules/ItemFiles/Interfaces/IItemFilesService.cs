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
        /// <param name="itemId">The Wiser item ID the image is linked to.</param>
        /// <param name="propertyName">The property name the image is linked to.</param>
        /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
        /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
        /// <param name="filename">The filename of the image as it's saved in Wiser. It is not case-sensitive.</param>
        /// <param name="fileNumber">Which image number should be retrieved in case there are more than one image linked to the same item with the same property name. Starts at 1.</param>
        /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
        /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
        /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
        /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
        /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemImageAsync(ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

        /// <summary>
        /// Attempts to retrieve an image linked to a link between two items. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemLinkId">The Wiser item link ID the image is linked to.</param>
        /// <param name="propertyName">The property name the image is linked to.</param>
        /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
        /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
        /// <param name="filename">The filename of the image as it's saved in Wiser. It is not case-sensitive.</param>
        /// <param name="fileNumber">Which image number should be retrieved in case there are more than one image linked to the same item with the same property name. Starts at 1.</param>
        /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
        /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
        /// <param name="encryptedItemLinkId">Optional: When the image is protected, the encrypted item link ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
        /// <param name="linkType">Optional: If there is a separate wiser_itemfile table for the specified item link, then enter the link type number here so that we can find it.</param>
        /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, int preferredWidth, int preferredHeight, string filename, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null, int linkType = 0);

        /// <summary>
        /// Attempts to retrieve an image directly, via file ID. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemId">The Wiser item file ID of the image.</param>
        /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
        /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
        /// <param name="filename">The filename of the image as it's saved in Wiser. It is not case-sensitive.</param>
        /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
        /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
        /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
        /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
        /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectImageAsync(ulong itemId, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

        /// <summary>
        /// Attempts to retrieve an image based on the image's filename. Will return a no-image if no image was found. A 404 status will be returned if a no-image file could not be found either.
        /// </summary>
        /// <param name="itemId">The Wiser item ID the image is linked to.</param>
        /// <param name="propertyName">The property name the image is linked to.</param>
        /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
        /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
        /// <param name="filename">The filename of the image as it's saved in Wiser. It is not case-sensitive.</param>
        /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
        /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
        /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
        /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
        /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
        Task<(byte[] fileBytes, DateTime lastModified)> GetWiserImageByFileNameAsync(ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

        /// <summary>
        /// Attempts to retrieve a file linked to an item. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="propertyName"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="encryptedItemId"></param>
        /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemFileAsync(ulong itemId, string propertyName, string filename, int fileNumber, string encryptedItemId = null, string entityType = null);

        /// <summary>
        /// Attempts to retrieve a file linked to a link between two items. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemLinkId"></param>
        /// <param name="propertyName"></param>
        /// <param name="filename"></param>
        /// <param name="fileNumber"></param>
        /// <param name="encryptedItemLinkId"></param>
        /// <param name="linkType">Optional: If there is a separate wiser_itemfile table for the specified item link, then enter the link type number here so that we can find it.</param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string filename, int fileNumber, string encryptedItemLinkId = null, int linkType = 0);

        /// <summary>
        /// Attempts to retrieve a file directly, via file ID. A 404 status will be returned if no file was found.
        /// </summary>
        /// <param name="itemId"></param>
        /// <param name="filename"></param>
        /// <param name="encryptedItemId"></param>
        /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
        /// <returns></returns>
        Task<(byte[] FileBytes, DateTime LastModified)> GetWiserDirectFileAsync(ulong itemId, string filename, string encryptedItemId = null, string entityType = null);
    }
}