using System.Xml.Serialization;
using GeeksCoreLibrary.Core.Enums;

namespace GeeksCoreLibrary.Core.Models;

[XmlType("HashSettings")]
public class HashSettingsModel
{
    /// <summary>
    /// Gets or sets the algorithm to use for hashing.
    /// </summary>
    public HashAlgorithms Algorithm { get; set; }

    /// <summary>
    /// Gets or sets the textual output of the hash.
    /// </summary>
    public HashRepresentations Representation { get; set; }
}