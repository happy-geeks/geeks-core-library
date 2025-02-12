using System;
using GeeksCoreLibrary.Modules.ItemFiles.Models;
using Newtonsoft.Json;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A model that represents a file that is stored in a Wiser database.
/// </summary>
public class WiserItemFileModel
{
    /// <summary>
    /// The unique identifier of the file.
    /// </summary>
    public ulong Id { get; set; }

    /// <summary>
    /// The unique identifier of the item that the file is linked to.
    /// Can be zero if the file is not linked to an item.
    /// </summary>
    public ulong ItemId { get; set; }

    /// <summary>
    /// The unique identifier of the item link that the file is linked to.
    /// Can be zero if the file is not linked to an item link.
    /// </summary>
    public ulong ItemLinkId { get; set; }

    /// <summary>
    /// The content type of the file (eg. "image/png" or "application/pdf").
    /// </summary>
    public string ContentType { get; set; }

    /// <summary>
    /// The bytes of the file. Can be <c>null</c> if the file contents are not stored in the database.
    /// </summary>
    [JsonIgnore]
    public byte[] Content { get; set; }

    /// <summary>
    /// The URL of the file. This will be <c>null</c> if the file contents are stored in the database.
    /// </summary>
    public string ContentUrl { get; set; }

    /// <summary>
    /// The name of the file, including the file extension.
    /// Special characters will be removed automatically when saving the file to the database.
    /// </summary>
    public string FileName { get; set; }

    /// <summary>
    /// The file extension, including the leading dot (eg. ".png").
    /// </summary>
    public string Extension { get; set; }

    /// <summary>
    /// The title of the file. This is used as the alt text for images on websites.
    /// </summary>
    public string Title { get; set; }

    /// <summary>
    /// The property name of the file. This is used to link the file to a specific property of an item.
    /// </summary>
    public string PropertyName { get; set; }

    /// <summary>
    /// The date and time when the file was added to the database.
    /// </summary>
    public DateTime AddedOn { get; set; }

    /// <summary>
    /// The username of the user who added the file to the database.
    /// </summary>
    public string AddedBy { get; set; }

    /// <summary>
    /// Whether the file is a protected file. Protected files are only accessible via encrypted IDs to prevent unauthorized access.
    /// The default value is <c>true</c>. Make sure to set it to <c>false</c> if the file should be publicly accessible.
    /// </summary>
    public bool Protected { get; set; } = true;

    /// <summary>
    /// Any extra data that this file has. This can be <c>null</c> if the file has no extra data.
    /// This is used to store things like alt texts in different languages.
    /// </summary>
    public WiserItemFileExtraDataModel ExtraData { get; set; }
}