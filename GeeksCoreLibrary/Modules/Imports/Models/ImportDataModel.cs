using System.Collections.Generic;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Modules.Imports.Models;

/// <summary>
/// A model for the import data for the Wiser import module.
/// </summary>
public class ImportDataModel
{
    /// <summary>
    /// Gets or sets the wiser item model.
    /// </summary>
    public WiserItemModel Item { get; set; }

    /// <summary>
    /// Gets or sets the item link import models.
    /// </summary>
    public List<ItemLinkImportModel> Links { get; set; } = new List<ItemLinkImportModel>();

    /// <summary>
    /// Gets or sets the wiser item file models.
    /// </summary>
    public List<WiserItemFileModel> Files { get; set; } = new List<WiserItemFileModel>();
}