using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;

namespace GeeksCoreLibrary.Modules.ItemFiles.Interfaces;

/// <summary>
/// A service for getting files from a Wiser database and for modifying them.
/// </summary>
public interface IItemFilesService
{
    /// <summary>
    /// Gets a file from the Wiser database. This method can be used to look up a file based on different criteria.
    /// </summary>
    /// <param name="lookupType">Indicate how to find the file.</param>
    /// <param name="id">The ID of the Wiser item or link. This needs to be an encrypted ID for protected files, otherwise it can be a plain ID.</param>
    /// <param name="propertyName">The property name of the file. This is used to link the file to a specific property of an item. Is mandatory for all lookup types, except <see cref="FileLookupTypes.ItemFileId"/> and <see cref="FileLookupTypes.ItemLinkFileId"/>.</param>
    /// <param name="fileName">The name of the file to get. Is mandatory if the <see cref="lookupType"/> is <see cref="FileLookupTypes.ItemFileName"/> or <see cref="FileLookupTypes.ItemLinkFileName"/>.</param>
    /// <param name="entityType">Optional: The entity type of the item that the file is linked too. Is needed when that entity type uses prefix tables.</param>
    /// <param name="linkType">Optional: The link type of the item link that the file is linked to. Is needed when that link type uses prefix tables.</param>
    /// <param name="fileNumber">Optional: The file number to get, This is based on the ordering of the files in the database. Default value is <c>1</c>.</param>
    /// <param name="includeContent">Optional: Whether to include the contents of the file. Default value is <c>true</c>.</param>
    /// <exception cref="ArgumentNullException">If one or more required parameters do not have a value.</exception>
    /// <exception cref="ArgumentOutOfRangeException">If an unknown lookup type is used.</exception>
    /// <returns>A <see cref="WiserItemFileModel"/> if the file was found, or <c>null</c> if it wasn't.</returns>
    Task<WiserItemFileModel> GetFileAsync(FileLookupTypes lookupType, object id, string propertyName = null, string fileName = null, string entityType = null, int linkType = 0, int fileNumber = 1, bool includeContent = true);

    /// <summary>
    /// <para>
    ///     Gets an image from the Wiser database and then resize it to the preferred width and height.
    ///     This method can be used to look up a file based on different criteria.
    /// </para>
    /// <para>
    ///     If the image could not be found, then it will attempt to return a fallback image. If that also fails, <c>null</c> will be returned.
    /// </para>
    /// </summary>
    /// <param name="lookupType">Indicate how to find the image.</param>
    /// <param name="id">The ID of the Wiser item or link. This needs to be an encrypted ID for protected files, otherwise it can be a plain ID.</param>
    /// <param name="fileName">The new file name for the image. If this contains a different file extension than the original file name in the database, then we will attempt to convert that image to it's new type.</param>
    /// <param name="propertyName">The property name of the file. This is used to link the file to a specific property of an item. Is mandatory for all lookup types, except <see cref="FileLookupTypes.ItemFileId"/> and <see cref="FileLookupTypes.ItemLinkFileId"/>.</param>
    /// <param name="entityType">Optional: The entity type of the item that the file is linked too. Is needed when that entity type uses prefix tables.</param>
    /// <param name="linkType">Optional: The link type of the item link that the file is linked to. Is needed when that link type uses prefix tables.</param>
    /// <param name="fileNumber">Optional: The file number to get, This is based on the ordering of the files in the database. Default value is <c>1</c>.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetResizedImageAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1, uint preferredWidth = 0, uint preferredHeight = 0, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center);

    /// <summary>
    /// Attempts to retrieve an image that is linked to a specific item.
    /// Will return a fallback image (if it exists) if no image was found.
    /// If a fallback image doesn't exist, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemId">The ID of the Wiser item that is file is linked to.</param>
    /// <param name="propertyName">The value of "property_name" of the wiser_itemfile table. If this is a file uploaded via Wiser, then the property_name of the corresponding field in Wiser will be the same.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and height the width might end up smaller than the preferred height.</param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="fileNumber">The ordering number of the file, if you want to get a file based on the ordering. Ordering always starts at 1.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserItemImageAsync(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

    /// <summary>
    /// Attempts to retrieve an image that is stored on a link between two Wiser items.
    /// Will return a fallback image (if it exists) if no image was found.
    /// If a fallback image doesn't exist, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemLinkId">The ID of the link between two Wiser items. This is the ID column from a wiser_itemfile table.</param>
    /// <param name="propertyName">The value of "property_name" of the wiser_itemfile table. If this is a file uploaded via Wiser, then the property_name of the corresponding field in Wiser will be the same.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height, the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and width, the height might end up smaller than the preferred height.</param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="fileNumber">The ordering number of the file, if you want to get a file based on the ordering. Ordering always starts at 1.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <param name="encryptedItemLinkId">Optional: When the image is protected, the encrypted item link ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="linkType">Optional: If there is a separate wiser_itemfile table for the specified item link, then enter the link type number here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserItemLinkImageAsync(ulong itemLinkId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, int fileNumber, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemLinkId = null, int linkType = 0);

    /// <summary>
    /// Attempts to retrieve an image via ID. This should be the ID from the wiser_itemfile table that contains this image.
    /// Will return a fallback image (if it exists) if no image was found.
    /// If a fallback image doesn't exist, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemId">The ID if the file itself.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height, the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and width, the height might end up smaller than the preferred height.</param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserDirectImageAsync(ulong itemId, uint preferredWidth, uint preferredHeight, string fileName, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

    /// <summary>
    /// Attempts to retrieve an image based on file name.
    /// Will return a fallback image (if it exists) if no image was found.
    /// If a fallback image doesn't exist, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemId">The Wiser item ID the image is linked to.</param>
    /// <param name="propertyName">The value of "property_name" of the wiser_itemfile table. If this is a file uploaded via Wiser, then the property_name of the corresponding field in Wiser will be the same.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height, the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and width, the height might end up smaller than the preferred height.</param>
    /// <param name="fileName">The file name of the file to get from the database. The file extension will be ignored and the name is case-insensitive.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <param name="encryptedItemId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserImageByFileNameAsync(ulong itemId, string propertyName, uint preferredWidth, uint preferredHeight, string fileName, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, string encryptedItemId = null, string entityType = null);

    /// <summary>
    /// Gets a file from the Wiser database and then download the contents if the file is a link to an external file.
    /// This method can be used to look up a file based on different criteria.
    /// If the file could not be found, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="lookupType">Indicate how to find the image.</param>
    /// <param name="id">The ID of the Wiser item or link. This needs to be an encrypted ID for protected files, otherwise it can be a plain ID.</param>
    /// <param name="fileName">The new file name for the image. If this contains a different file extension than the original file name in the database, then we will attempt to convert that image to it's new type.</param>
    /// <param name="propertyName">The property name of the file. This is used to link the file to a specific property of an item. Is mandatory for all lookup types, except <see cref="FileLookupTypes.ItemFileId"/> and <see cref="FileLookupTypes.ItemLinkFileId"/>.</param>
    /// <param name="entityType">Optional: The entity type of the item that the file is linked too. Is needed when that entity type uses prefix tables.</param>
    /// <param name="linkType">Optional: The link type of the item link that the file is linked to. Is needed when that link type uses prefix tables.</param>
    /// <param name="fileNumber">Optional: The file number to get, This is based on the ordering of the files in the database. Default value is <c>1</c>.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetParsedFileAsync(FileLookupTypes lookupType, object id, string fileName, string propertyName = null, string entityType = null, int linkType = 0, int fileNumber = 1);

    /// <summary>
    /// Attempts to retrieve a file that is stored on a link between two Wiser items.
    /// If the file could not be found, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemId">The Wiser item ID the file is linked to.</param>
    /// <param name="propertyName">The value of "property_name" of the wiser_itemfile table. If this is a file uploaded via Wiser, then the property_name of the corresponding field in Wiser will be the same.</param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="fileNumber">The ordering number of the file, if you want to get a file based on the ordering. Ordering always starts at 1.</param>
    /// <param name="encryptedItemId">Optional: When the file is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserItemFileAsync(ulong itemId, string propertyName, string fileName, int fileNumber, string encryptedItemId = null, string entityType = null);

    /// <summary>
    /// Attempts to retrieve a file that is stored on a link between two Wiser items.
    /// If the file could not be found, then <c>null</c> will be returned.
    /// </summary>
    /// <param name="itemLinkId">The Wiser item ID the file is linked to.</param>
    /// <param name="propertyName">The value of "property_name" of the wiser_itemfile table. If this is a file uploaded via Wiser, then the property_name of the corresponding field in Wiser will be the same.</param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="fileNumber">The ordering number of the file, if you want to get a file based on the ordering. Ordering always starts at 1.</param>
    /// <param name="encryptedItemLinkId">Optional: When the image is protected, the encrypted item ID needs to be provided to retrieve it. Leave it on <c>null</c> if the image is not protected.</param>
    /// <param name="linkType">Optional: If there is a separate wiser_itemfile table for the specified item link, then enter the link type number here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserItemLinkFileAsync(ulong itemLinkId, string propertyName, string fileName, int fileNumber, string encryptedItemLinkId = null, int linkType = 0);

    /// <summary>
    /// Attempts to retrieve a file directly, via file ID. A 404 status will be returned if no file was found.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="fileName">The file name to use for caching the file. We will generate a file name with all parameters of this method in it. The value of this fileName parameter will be added at the end of the full file name, so make sure this contains the file extension.</param>
    /// <param name="encryptedItemId"></param>
    /// <param name="entityType">Optional: If there is a separate wiser_itemfile table for the specified item, then enter the entity type here so that we can find it.</param>
    /// <returns>A value tuple containing the bytes of the image file and the last modified date.</returns>
    Task<FileResultModel> GetWiserDirectFileAsync(ulong itemId, string fileName, string encryptedItemId = null, string entityType = null);

    /// <summary>
    /// Converts the data into an image of the given size, format, and quality, and will return the bytes of that image.
    /// </summary>
    /// <param name="file">The file data from the database.</param>
    /// <param name="preferredWidth">The preferred width the image should be resized to. Based on the resize mode and height, the width might end up smaller than the preferred width.</param>
    /// <param name="preferredHeight">The preferred height the image should be resized to. Based on the resize mode and width, the height might end up smaller than the preferred height.</param>
    /// <param name="resizeMode">Optional: The resize mode to use. Refer to documentation to learn what the different resize modes do. The default is <see cref="ResizeModes.Normal"/>.</param>
    /// <param name="anchorPosition">Optional: The anchor position to use when the resizing of the images causes it to be cropped. The default is <see cref="AnchorPositions.Center"/>.</param>
    /// <returns>The byte array for the resized image, or <c>null</c> if something went wrong, and the datetime when the file was last updated.</returns>
    Task<FileResultModel> HandleImageAsync(WiserItemFileModel file, uint preferredWidth, uint preferredHeight, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center);

    /// <summary>
    /// Handles the file data from the database. If the file is a link to an external file, this will attempt to download the file and return the bytes.
    /// </summary>
    /// <param name="file">The file data from the database.</param>
    /// <returns>The byte array for image, or <c>null</c> if something went wrong. Also returns the datetime when the file was last updated.</returns>
    Task<FileResultModel> HandleFileAsync(WiserItemFileModel file);
}