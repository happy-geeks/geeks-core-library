using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Models;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Services;

public class ExcelService : IExcelService, IScopedService
{
    /// <summary>
    /// Excel worksheets are hard-limited to this many rows (including the header).
    /// </summary>
    private const uint ExcelMaxRows = 1_048_576;

    /// <inheritdoc />
    public byte[] JsonArrayToExcel(JArray data, string sheetName = "Data")
    {
        using var memoryStream = new MemoryStream();
        if (data == null || !data.Any())
        {
            return memoryStream.ToArray();
        }

        data = JsonHelpers.FlattenJsonArray(data);
        var columnNames = JsonHelpers.GetUniquePropertyNames(data);

        using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
        {
            var spreadsheetDocumentReferences = PrepareSpreadsheetDocument(spreadsheetDocument, sheetName);

            // Add column names.
            var rowColumnNames = columnNames.Cast<object>().ToList();

            AddRow(rowColumnNames, 1, spreadsheetDocumentReferences);

            // Add data.
            uint currentRow = 2;

            foreach (var jsonToken in data)
            {
                var rowColumnValues = new List<object>();

                if (jsonToken is JObject jsonObject)
                {
                    foreach (var subToken in jsonObject)
                    {
                        rowColumnValues.Add(subToken.Value);
                    }
                }

                AddRow(rowColumnValues, currentRow, spreadsheetDocumentReferences);
                currentRow++;
            }

            // Add filters on the columns.
            var autoFilter = new AutoFilter
            {
                Reference = StringValue.FromString($"A1:{GetColumnNameFromIndex(rowColumnNames.Count - 1)}{currentRow - 1}")
            };

            spreadsheetDocumentReferences.Worksheet.Append(autoFilter);
        }

        return memoryStream.ToArray();
    }

    /// <inheritdoc />
    public async Task DbDataReaderToExcelAsync(DbDataReader dataReader, Stream outputStream, string sheetName = "Data", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (dataReader.FieldCount == 0)
        {
            return;
        }

        // OpenXML builds a ZIP package and requires a readable, seekable stream.
        // HTTP response bodies are typically write-only and non-seekable, so buffer via a temp file.
        if (!outputStream.CanRead || !outputStream.CanSeek)
        {
            var tempPath = Path.Combine(Path.GetTempPath(), $"gcl-excel-{Guid.NewGuid():N}.xlsx");
            await using (var tempStream = new FileStream(
                             tempPath,
                             FileMode.CreateNew,
                             FileAccess.ReadWrite,
                             FileShare.None,
                             bufferSize: 64 * 1024,
                             FileOptions.Asynchronous | FileOptions.SequentialScan | FileOptions.DeleteOnClose))
            {
                await WriteWorkbookToPackageStreamAsync(dataReader, tempStream, sheetName, leavePackageStreamOpen: true, cancellationToken);
                tempStream.Position = 0;
                await tempStream.CopyToAsync(outputStream, cancellationToken);
            }

            return;
        }

        await WriteWorkbookToPackageStreamAsync(dataReader, outputStream, sheetName, leavePackageStreamOpen: true, cancellationToken);
    }

    /// <summary>
    /// Creates the .xlsx package on a readable/seekable stream using OpenXmlWriter for row data.
    /// </summary>
    private async Task WriteWorkbookToPackageStreamAsync(
        DbDataReader dataReader,
        Stream packageStream,
        string sheetName,
        bool leavePackageStreamOpen,
        CancellationToken cancellationToken)
    {
        // SpreadsheetDocument.Dispose closes its stream; optionally keep the caller's stream open.
        Stream streamForPackage = leavePackageStreamOpen ? new NonClosingStream(packageStream) : packageStream;
        try
        {
            using var spreadsheetDocument = SpreadsheetDocument.Create(streamForPackage, SpreadsheetDocumentType.Workbook);

            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            workbookPart.Workbook = new Workbook(new FileVersion { ApplicationName = "Microsoft Office Excel" });

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            var fieldCount = dataReader.FieldCount;
            uint currentRow = 1;

            using (var writer = OpenXmlWriter.Create(worksheetPart))
            {
                writer.WriteStartElement(new Worksheet());
                writer.WriteStartElement(new SheetData());

                // Header row
                writer.WriteStartElement(new Row { RowIndex = currentRow });
                for (var i = 0; i < fieldCount; i++)
                {
                    WriteInlineStringCell(writer, GetColumnNameFromIndex(i) + currentRow, dataReader.GetName(i));
                }
                writer.WriteEndElement(); // Row

                while (await dataReader.ReadAsync(cancellationToken))
                {
                    currentRow++;
                    if (currentRow > ExcelMaxRows)
                    {
                        throw new InvalidOperationException($"Excel worksheets are limited to {ExcelMaxRows} rows.");
                    }

                    writer.WriteStartElement(new Row { RowIndex = currentRow });
                    for (var i = 0; i < fieldCount; i++)
                    {
                        var cellReference = GetColumnNameFromIndex(i) + currentRow;
                        if (dataReader.IsDBNull(i))
                        {
                            WriteInlineStringCell(writer, cellReference, String.Empty);
                            continue;
                        }

                        WriteCellValue(writer, cellReference, dataReader.GetValue(i));
                    }
                    writer.WriteEndElement(); // Row
                }

                writer.WriteEndElement(); // SheetData

                if (fieldCount > 0 && currentRow >= 1)
                {
                    writer.WriteElement(new AutoFilter
                    {
                        Reference = StringValue.FromString($"A1:{GetColumnNameFromIndex(fieldCount - 1)}{currentRow}")
                    });
                }

                writer.WriteEndElement(); // Worksheet
            }

            var sheets = workbookPart.Workbook.AppendChild(new Sheets());
            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = sheetName
            });

            workbookPart.Workbook.Save();
        }
        finally
        {
            if (leavePackageStreamOpen)
            {
                await streamForPackage.DisposeAsync();
            }
        }
    }

    /// <summary>
    /// Prepare the spreadsheet document.
    /// </summary>
    /// <param name="spreadsheetDocument">The spreadsheet document that needs to be filled.</param>
    /// <param name="sheetName">The name of the sheet.</param>
    /// <returns></returns>
    private SpreadsheetDocumentReferencesModel PrepareSpreadsheetDocument(SpreadsheetDocument spreadsheetDocument, string sheetName)
    {
        var spreadsheetDocumentReferences = new SpreadsheetDocumentReferencesModel();
        var workbookPart = spreadsheetDocument.AddWorkbookPart();

        // WorkbookPart > Workbook > Sheets > Sheet
        var worksheetPart = spreadsheetDocument.WorkbookPart!.AddNewPart<WorksheetPart>();
        var fileVersion = new FileVersion();
        var sheet = new Sheet();
        var sharedStringTablePart = spreadsheetDocument.WorkbookPart.AddNewPart<SharedStringTablePart>();
        sharedStringTablePart.SharedStringTable = new SharedStringTable();

        sheet.Name = sheetName;
        sheet.SheetId = 1;
        sheet.Id = workbookPart.GetIdOfPart(worksheetPart);

        fileVersion.ApplicationName = "Microsoft Office Excel";

        worksheetPart.Worksheet = new Worksheet(new SheetData());
        worksheetPart.Worksheet.Save();

        spreadsheetDocument.WorkbookPart.Workbook = new Workbook(fileVersion, new Sheets(sheet));
        spreadsheetDocument.WorkbookPart.Workbook.Save();

        // Store the references for quick access later.
        spreadsheetDocumentReferences.Sheet = sheet;
        spreadsheetDocumentReferences.WorkbookPart = spreadsheetDocument.WorkbookPart;
        spreadsheetDocumentReferences.Worksheet = worksheetPart.Worksheet;
        spreadsheetDocumentReferences.SheetData = spreadsheetDocumentReferences.Worksheet.GetFirstChild<SheetData>();
        if (spreadsheetDocumentReferences.WorkbookPart.SharedStringTablePart != null)
        {
            spreadsheetDocumentReferences.SharedStringTable = spreadsheetDocumentReferences.WorkbookPart.SharedStringTablePart.SharedStringTable;
        }

        if (spreadsheetDocumentReferences.SharedStringTable != null)
        {
            spreadsheetDocumentReferences.LastSharedStringIndex = spreadsheetDocumentReferences.SharedStringTable.Elements<SharedStringItem>().Count() - 1; // Used to keep track of the index of the last shared string while adding rows.
        }

        return spreadsheetDocumentReferences;
    }

    /// <summary>
    /// Get the name of the column.
    /// Limited to index 0 - 675 / A - ZZ.
    /// </summary>
    /// <param name="columnIndex">THe index of the column.</param>
    /// <returns></returns>
    private string GetColumnNameFromIndex(int columnIndex)
    {
        string columnName;

        if (columnIndex >= 26) // Two letter column.
        {
            var tempIndex = Convert.ToInt32(Math.Floor(columnIndex / 26d) - 1);
            columnName = GetColumnNameFromIndex(tempIndex);
            columnName += Char.ConvertFromUtf32(columnIndex - ((tempIndex + 1) * 26) + 65);
        }
        else // One letter column.
        {
            columnName = ((char) (columnIndex + 65)).ToString();
        }

        return columnName;
    }

    /// <summary>
    /// Get the index of a column based on its name.
    /// </summary>
    /// <param name="columnName">The name of the column, eg. A, B, AZ.</param>
    /// <returns></returns>
    private int GetColumnIndexFromName(object columnName)
    {
        var columnIndex = 0;

        if (columnName is not string columnNameString)
        {
            return columnIndex;
        }

        var columnNameCharArray = columnNameString.ToCharArray();

        for (var i = 0; i < columnNameCharArray.Length; i++)
        {
            columnIndex += (columnNameCharArray[i] - 64) * (int) Math.Pow(26, columnNameCharArray.Length - i - 1);
        }

        return columnIndex;
    }

    /// <summary>
    /// Add a row to the Excel file.
    /// </summary>
    /// <param name="rowColumnValues">The values of the columns.</param>
    /// <param name="rowNumber">THe number of the row.</param>
    /// <param name="spreadsheetDocumentReferences">The references in the spreadsheet document.</param>
    public void AddRow(List<object> rowColumnValues, uint rowNumber, SpreadsheetDocumentReferencesModel spreadsheetDocumentReferences)
    {
        if (spreadsheetDocumentReferences.Sheet == null)
        {
            return;
        }

        var row = new Row
        {
            RowIndex = rowNumber
        };

        for (var i = 0; i < rowColumnValues.Count; i++)
        {
            var cell = new Cell
            {
                CellReference = GetColumnNameFromIndex(i) + rowNumber
            };

            var rawValue = rowColumnValues[i];
            if (rawValue == null || rawValue is JValue { Type: JTokenType.Null })
            {
                AppendSharedStringCell(cell, spreadsheetDocumentReferences, String.Empty);
                row.Append(cell);
                continue;
            }

            var columnType = rawValue.GetType().ToString().ToLowerInvariant();
            var unwrapped = rawValue;
            if (rawValue is JValue jsonValue) // If the type is JValue get the original type.
            {
                columnType = jsonValue.Type.ToString().ToLowerInvariant();
                unwrapped = jsonValue.Value;
            }

            switch (columnType)
            {
                case "system.byte":
                case "system.sbyte":
                case "system.int16":
                case "system.uint16":
                case "system.int32":
                case "system.uint32":
                case "system.int64":
                case "system.uint64":
                case "system.single":
                case "system.double":
                case "system.decimal":
                case "integer":
                case "float":
                    cell.CellValue = new CellValue(Convert.ToString(unwrapped, CultureInfo.InvariantCulture) ?? String.Empty);
                    cell.DataType = CellValues.Number;
                    break;

                case "system.datetime":
                case "system.datetimeoffset":
                case "datetime":
                case "date":
                    // Write ISO-8601 text instead of StyleIndex dates (no WorkbookStylesPart is created).
                    var dateTime = unwrapped is DateTimeOffset dateTimeOffset
                        ? dateTimeOffset.UtcDateTime
                        : Convert.ToDateTime(unwrapped, CultureInfo.InvariantCulture);
                    AppendSharedStringCell(cell, spreadsheetDocumentReferences, dateTime.ToString("o", CultureInfo.InvariantCulture));
                    break;

                default:
                    AppendSharedStringCell(cell, spreadsheetDocumentReferences, Convert.ToString(unwrapped, CultureInfo.InvariantCulture) ?? String.Empty);
                    break;
            }

            row.Append(cell);
        }

        spreadsheetDocumentReferences.SheetData.Append(row);
    }

    private static void AppendSharedStringCell(Cell cell, SpreadsheetDocumentReferencesModel spreadsheetDocumentReferences, string text)
    {
        spreadsheetDocumentReferences.SharedStringTable.Append(new SharedStringItem(new Text(SanitizeXmlText(text))));
        spreadsheetDocumentReferences.LastSharedStringIndex++;
        cell.CellValue = new CellValue(spreadsheetDocumentReferences.LastSharedStringIndex.ToString(CultureInfo.InvariantCulture));
        cell.DataType = CellValues.SharedString;
    }

    /// <inheritdoc />
    public List<string> GetColumnNames(Stream excelStream)
    {
        using var stream = SpreadsheetDocument.Open(excelStream, false);
        return GetColumnNames(stream);
    }

    /// <inheritdoc />
    public List<string> GetColumnNames(string filePath)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);
        return GetColumnNames(document);
    }

    /// <summary>
    /// Get the values of each cell from the first row as column names.
    /// </summary>
    /// <param name="document">The SpreadsheetDocument to read from.</param>
    /// <returns></returns>
    private List<string> GetColumnNames(SpreadsheetDocument document)
    {
        var columnNames = new List<string>();

        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            return columnNames;
        }

        var sharedStringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
        var sharedStringTable = sharedStringTablePart.SharedStringTable;

        var worksheetPart = workbookPart.WorksheetParts.First();
        var sheet = worksheetPart.Worksheet;

        var row = sheet.Descendants<Row>().First();

        foreach (var cell in row.Elements<Cell>())
        {
            if (cell.CellValue == null)
            {
                continue;
            }

            if (cell.DataType != null && cell.DataType == CellValues.SharedString)
            {
                var index = Int32.Parse(cell.CellValue.Text);
                columnNames.Add(sharedStringTable.ChildElements[index].InnerText);
            }
            else
            {
                columnNames.Add("");
            }
        }

        return columnNames;
    }

    /// <inheritdoc />
    public int GetRowCount(Stream excelStream)
    {
        using var stream = SpreadsheetDocument.Open(excelStream, false);
        return GetRowCount(stream);
    }

    /// <inheritdoc />
    public int GetRowCount(string filePath)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);
        return GetRowCount(document);
    }

    /// <summary>
    /// Get the number of rows in the file.
    /// </summary>
    /// <param name="document">The SpreadsheetDocument to read from.</param>
    /// <returns></returns>
    private int GetRowCount(SpreadsheetDocument document)
    {
        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            return 0;
        }

        var worksheetPart = workbookPart.WorksheetParts.First();
        var sheet = worksheetPart.Worksheet;

        return sheet.Descendants<Row>().Count();
    }

    /// <inheritdoc />
    public List<List<string>> GetLines(Stream excelStream, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false)
    {
        using var stream = SpreadsheetDocument.Open(excelStream, false);
        return GetLines(stream, numberOfColumns, skipFirstLine, firstColumnAreIds);
    }

    /// <inheritdoc />
    public List<List<string>> GetLines(string filePath, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false)
    {
        using var document = SpreadsheetDocument.Open(filePath, false);
        return GetLines(document, numberOfColumns, skipFirstLine, firstColumnAreIds);
    }

    /// <summary>
    /// Get the lines from the Excel file.
    /// </summary>
    /// <param name="document">The SpreadsheetDocument to read from.</param>
    /// <param name="numberOfColumns">The number of columns that need to be retrieved for each row.</param>
    /// <param name="skipFirstLine">Optional: If set to true the first line will not be included in the results.</param>
    /// <param name="firstColumnAreIds">If the first column are for IDs of Wiser Item the default value will be set on "0" to indicate a new item instead of an empty string.</param>
    /// <returns></returns>
    public List<List<string>> GetLines(SpreadsheetDocument document, int numberOfColumns, bool skipFirstLine = false, bool firstColumnAreIds = false)
    {
        var result = new List<List<string>>();

        var workbookPart = document.WorkbookPart;
        if (workbookPart == null)
        {
            return result;
        }

        var sharedStringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
        var sharedStringTable = sharedStringTablePart.SharedStringTable;

        var worksheetPart = workbookPart.WorksheetParts.First();
        var sheet = worksheetPart.Worksheet;

        var rows = sheet.Descendants<Row>();
        var firstRow = true;

        foreach (var row in rows)
        {
            if (firstRow && skipFirstLine)
            {
                firstRow = false;
                continue;
            }

            var columns = new List<string>();

            // Create an entry for each column to ensure the correct number of columns in the row. Cells are only returned if they have a value.
            for (var i = 0; i < numberOfColumns; i++)
            {
                // If the first column are ids, the first column is always 0. When the import overrides existing items the value will be overwritten when reading the cells.
                if (i == 0 && firstColumnAreIds)
                {
                    columns.Add("0");
                }
                else
                {
                    columns.Add("");
                }
            }

            foreach (var cell in row.Elements<Cell>())
            {
                if (cell.CellReference?.Value == null)
                {
                    continue;
                }

                // Get the column index of the cell based on its name (e.g. A1).
                var columnName = StringHelpers.RemoveDigitsAndHyphens(cell.CellReference.Value);
                var columnIndex = GetColumnIndexFromName(columnName);

                if (cell.CellValue != null && cell.DataType != null && cell.DataType == CellValues.SharedString)
                {
                    var index = Int32.Parse(cell.CellValue.Text);
                    columns[columnIndex - 1] = sharedStringTable.ChildElements[index].InnerText;
                }
                else
                {
                    columns[columnIndex - 1] = cell.CellValue?.Text;
                }
            }

            result.Add(columns);
            firstRow = false;
        }

        return result;
    }

    private static void WriteCellValue(OpenXmlWriter writer, string cellReference, object value)
    {
        switch (value)
        {
            case byte or sbyte or short or ushort or int or uint or long or ulong or float or double or decimal:
                writer.WriteElement(new Cell
                {
                    CellReference = cellReference,
                    DataType = CellValues.Number,
                    CellValue = new CellValue(Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty)
                });
                break;

            case DateTime dateTime:
                WriteInlineStringCell(writer, cellReference, dateTime.ToString("o", CultureInfo.InvariantCulture));
                break;

            case DateTimeOffset dateTimeOffset:
                WriteInlineStringCell(writer, cellReference, dateTimeOffset.UtcDateTime.ToString("o", CultureInfo.InvariantCulture));
                break;

            case bool boolean:
                writer.WriteElement(new Cell
                {
                    CellReference = cellReference,
                    DataType = CellValues.Boolean,
                    CellValue = new CellValue(boolean)
                });
                break;

            default:
                WriteInlineStringCell(writer, cellReference, Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty);
                break;
        }
    }

    private static void WriteInlineStringCell(OpenXmlWriter writer, string cellReference, string text)
    {
        var cell = new Cell
        {
            CellReference = cellReference
        };
        SetInlineStringCell(cell, text);
        writer.WriteElement(cell);
    }

    private static void SetInlineStringCell(Cell cell, string text)
    {
        cell.DataType = CellValues.InlineString;
        cell.InlineString = new InlineString(new Text(SanitizeXmlText(text)));
    }

    /// <summary>
    /// Removes characters that are illegal in XML 1.0 so OpenXML serialization cannot throw on dispose/save.
    /// </summary>
    private static string SanitizeXmlText(string value)
    {
        if (String.IsNullOrEmpty(value))
        {
            return value ?? String.Empty;
        }

        StringBuilder builder = null;
        for (var i = 0; i < value.Length; i++)
        {
            var character = value[i];
            if (XmlConvert.IsXmlChar(character))
            {
                builder?.Append(character);
                continue;
            }

            if (builder == null)
            {
                builder = new StringBuilder(value.Length);
                builder.Append(value, 0, i);
            }
        }

        return builder?.ToString() ?? value;
    }

    /// <summary>
    /// Forwards all operations to an inner stream but does not dispose it.
    /// Needed because <see cref="SpreadsheetDocument"/> closes its stream on dispose.
    /// </summary>
    private sealed class NonClosingStream : Stream
    {
        private readonly Stream inner;

        public NonClosingStream(Stream inner)
        {
            this.inner = inner;
        }

        public override bool CanRead => inner.CanRead;
        public override bool CanSeek => inner.CanSeek;
        public override bool CanWrite => inner.CanWrite;
        public override long Length => inner.Length;

        public override long Position
        {
            get => inner.Position;
            set => inner.Position = value;
        }

        public override void Flush() => inner.Flush();

        public override Task FlushAsync(System.Threading.CancellationToken cancellationToken) =>
            inner.FlushAsync(cancellationToken);

        public override int Read(byte[] buffer, int offset, int count) =>
            inner.Read(buffer, offset, count);

        public override long Seek(long offset, SeekOrigin origin) =>
            inner.Seek(offset, origin);

        public override void SetLength(long value) =>
            inner.SetLength(value);

        public override void Write(byte[] buffer, int offset, int count) =>
            inner.Write(buffer, offset, count);

        public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, System.Threading.CancellationToken cancellationToken = default) =>
            inner.WriteAsync(buffer, cancellationToken);

        protected override void Dispose(bool disposing)
        {
            // Intentionally do not dispose the inner stream.
        }
    }
}
