using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Imports.Models;

/// <summary>
/// A model for the item link import in the Wiser import module.
/// </summary>
public class ItemLinkImportModel : WiserItemLinkModel
{
    /// <summary>
    /// Gets or sets whether to delete existing links of the imported item, before importing new links, therefor replacing any old links with new ones.
    /// </summary>
    public bool DeleteExistingLinks { get; set; }
}