using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;

namespace GeeksCoreLibrary.Modules.Exports.Models;

/// <summary>
/// A model to hold references to objects within the <see cref="SpreadsheetDocument"/> for quick access.
/// </summary>
public class SpreadsheetDocumentReferencesModel
{
    /// <summary>
    /// Gets or sets the sheet of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public Sheet Sheet { get; set; }

    /// <summary>
    /// Gets or sets the workbook part of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public WorkbookPart WorkbookPart { get; set; }

    /// <summary>
    /// Gets or sets the worksheet of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public Worksheet Worksheet { get; set; }

    /// <summary>
    /// Gets or sets the sheet data of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public SheetData SheetData { get; set; }

    /// <summary>
    /// Gets or sets the shared string table of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public SharedStringTable SharedStringTable { get; set; }

    /// <summary>
    /// Gets or sets the last shared string index of the active <see cref="SpreadsheetDocument"/>.
    /// </summary>
    public int LastSharedStringIndex { get; set; }
}