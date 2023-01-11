using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using GeeksCoreLibrary.Modules.Exports.Models;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Services
{
    public class ExcelService : IExcelService, IScopedService
    {
        /// <inheritdoc />
        public byte[] JsonArrayToExcel(JArray data, string sheetName = "Data")
        {
            using var memoryStream = new MemoryStream();

            using (var spreadsheetDocument = SpreadsheetDocument.Create(memoryStream, SpreadsheetDocumentType.Workbook))
            {
                var spreadsheetDocumentReferences = PrepareSpreadsheetDocument(spreadsheetDocument, sheetName);

                if (data != null)
                {
                    // Add column names.
                    var rowColumnNames = new List<object>();

                    foreach (var jsonToken in data.First as JObject)
                    {
                        rowColumnNames.Add(jsonToken.Key);
                    }

                    AddRow(rowColumnNames, 1, spreadsheetDocumentReferences);

                    // Add data.
                    uint currentRow = 2;

                    foreach (JObject jsonObject in data)
                    {
                        var rowColumnValues = new List<object>();

                        foreach (var jsonToken in jsonObject)
                        {
                            rowColumnValues.Add(jsonToken.Value);
                        }

                        AddRow(rowColumnValues, currentRow, spreadsheetDocumentReferences);
                        currentRow++;
                    }

                    // Add filters on the columns.
                    var autoFilter = new AutoFilter()
                    {
                        Reference = StringValue.FromString(
                            $"A1:{GetColumnNameFromIndex(rowColumnNames.Count - 1)}{currentRow - 1}")
                    };

                    spreadsheetDocumentReferences.Worksheet.Append(autoFilter);
                }
            }

            return memoryStream.ToArray();
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

            // WorkbookPart > Workbook > Sheets > Sheet
            var workbookPart = spreadsheetDocument.AddWorkbookPart();
            var worksheetPart = spreadsheetDocument.WorkbookPart.AddNewPart<WorksheetPart>();
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
            spreadsheetDocumentReferences.SharedStringTable = spreadsheetDocumentReferences.WorkbookPart.SharedStringTablePart.SharedStringTable;
            spreadsheetDocumentReferences.LastSharedStringIndex = spreadsheetDocumentReferences.SharedStringTable.Elements<SharedStringItem>().Count() - 1; // Used to keep track of the index of the last shared string while adding rows.

            return spreadsheetDocumentReferences;
        }

        /// <summary>
        /// Get the name of the column.
        /// Limited to index 0 - 675 / A - ZZ.
        /// </summary>
        /// <param name="columnIndex">THe index of the column.</param>
        /// <returns></returns>
        public string GetColumnNameFromIndex(int columnIndex)
        {
            string columnName;

            if (columnIndex >= 26) // Two letter column.
            {
                var tempIndex = Convert.ToInt32(Math.Floor(columnIndex / 26d) - 1);
                columnName = GetColumnNameFromIndex(tempIndex);
                columnName += char.ConvertFromUtf32(columnIndex - ((tempIndex + 1) * 26) + 65);
            }
            else // One letter column.
            {
                columnName = ((char)(columnIndex + 65)).ToString();
            }

            return columnName;
        }

        /// <summary>
        /// Add a row to the Excel file.
        /// </summary>
        /// <param name="rowColumnValues">The values of the columns.</param>
        /// <param name="rowNumber">THe number of the row.</param>
        /// <param name="spreadsheetDocumentReferences">The references in the spreadsheet document.</param>
        public void AddRow(List<object> rowColumnValues, uint rowNumber, SpreadsheetDocumentReferencesModel spreadsheetDocumentReferences)
        {
            if (spreadsheetDocumentReferences.Sheet != null)
            {
                var row = new Row
                {
                    RowIndex = rowNumber
                };

                for (var i = 0; i < rowColumnValues.Count; i++)
                {
                    var cell = new Cell();
                    cell.CellReference = GetColumnNameFromIndex(i) + rowNumber;

                    var columnType = rowColumnValues[i].GetType().ToString().ToLower();
                    if (rowColumnValues[i] is JValue jsonValue) // If the type is JValue get the original type.
                    {
                        columnType = jsonValue.Type.ToString().ToLower();
                    }

                    switch (columnType)
                    {
                        case "system.int32":
                        case "system.int64":
                        case "system.double":
                        case "system.sbyte":
                        case "integer":
                        case "float":
                            cell.CellValue = new CellValue(rowColumnValues[0].ToString().Replace(",", "."));
                            cell.DataType = CellValues.Number;
                            break;

                        case "system.datetime":
                            var dateTimeEpoch = new DateTime(1900, 1, 1, 0, 0, 0, 0);
                            var dateTime = Convert.ToDateTime(rowColumnValues[i]);
                            var timeSpan = dateTime - dateTimeEpoch;
                            double excelDateTime;

                            if (timeSpan.Days >= 59)
                            {
                                excelDateTime = timeSpan.TotalDays + 2.0;
                            }
                            else
                            {
                                excelDateTime = timeSpan.TotalDays + 1.0;
                            }

                            cell.StyleIndex = 2;
                            cell.CellValue = new CellValue(excelDateTime.ToString().Replace(",", "."));
                            break;
                        default:
                            spreadsheetDocumentReferences.SharedStringTable.Append(new SharedStringItem(new Text(rowColumnValues[i].ToString())));
                            spreadsheetDocumentReferences.LastSharedStringIndex++;
                            cell.CellValue = new CellValue(spreadsheetDocumentReferences.LastSharedStringIndex.ToString());
                            cell.DataType = CellValues.SharedString;
                            break;
                    }

                    row.Append(cell);
                }

                spreadsheetDocumentReferences.SheetData.Append(row);
            }
        }

        /// <inheritdoc />
        public List<string> GetColumnNames(string filePath)
        {
            var columnNames = new List<string>();
            
            using (var document = SpreadsheetDocument.Open(filePath, false))
            {
                var workbookPart = document.WorkbookPart;
                var sharedStringTablePart = workbookPart.GetPartsOfType<SharedStringTablePart>().First();
                var sharedStringTable = sharedStringTablePart.SharedStringTable;

                var worksheetPart = workbookPart.WorksheetParts.First();
                var sheet = worksheetPart.Worksheet;

                var row = sheet.Descendants<Row>().First();
                
                 foreach (Cell cell in row.Elements<Cell>())
                 {
                     if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                     {
                         int index = int.Parse(cell.CellValue.Text);
                         columnNames.Add(sharedStringTable.ChildElements[index].InnerText);
                     }
                     else if (cell.CellValue != null)
                     {
                         columnNames.Add("");
                     }
                 }
            }
            
            return columnNames;
        }

        /// <inheritdoc />
        public int GetRowCount(string filePath)
        {
            using (var document = SpreadsheetDocument.Open(filePath, false))
            {
                var workbookPart = document.WorkbookPart;
                var worksheetPart = workbookPart.WorksheetParts.First();
                var sheet = worksheetPart.Worksheet;

                return sheet.Descendants<Row>().Count();
            }
        }

        /// <inheritdoc />
        public List<List<string>> GetLines(string filePath, bool skipFirstLine = false)
        {
            var result = new List<List<string>>();

            using (var document = SpreadsheetDocument.Open(filePath, false))
            {
                var workbookPart = document.WorkbookPart;
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
                    
                    foreach (Cell cell in row.Elements<Cell>())
                    {
                        if (cell.DataType != null && cell.DataType == CellValues.SharedString)
                        {
                            int index = int.Parse(cell.CellValue.Text);
                            columns.Add(sharedStringTable.ChildElements[index].InnerText);
                        }
                        else if (cell.CellValue != null)
                        {
                            columns.Add("");
                        }
                    }

                    result.Add(columns);
                    firstRow = false;
                }
            }
            
            return result;
        }
    }
}
