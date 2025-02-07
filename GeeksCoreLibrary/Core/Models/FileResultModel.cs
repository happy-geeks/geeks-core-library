using System;

namespace GeeksCoreLibrary.Core.Models;

/// <summary>
/// A model that represents a file from Wiser, after it has been parsed/processed.
/// </summary>
public class FileResultModel
{
    /// <summary>
    /// The contents of the file as they should be returned to the client.
    /// </summary>
    public byte[] FileBytes { get; set; }

    /// <summary>
    /// The date and time that the file was last modified, for caching purposes.
    /// </summary>
    public DateTime LastModified { get; set; }

    /// <summary>
    /// The original file data from the database.
    /// </summary>
    public WiserItemFileModel WiserItemFile { get; set; }
}