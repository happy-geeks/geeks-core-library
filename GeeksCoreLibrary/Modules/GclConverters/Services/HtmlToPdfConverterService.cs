using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Data;
using System.IO;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Enums;
using GeeksCoreLibrary.Modules.GclConverters.Models;

namespace GeeksCoreLibrary.Modules.GclConverters.Services;

/// <summary>
/// This is a base class for converting HTML to PDF files.
/// This class contains all the default functionality for retrieving settings from database, storing images etc.
/// However, the actual conversion of HTML to PDF should be implemented in a derived class.
/// </summary>
public class HtmlToPdfConverterService(IObjectsService objectsService, IDatabaseConnection databaseConnection, IWebHostEnvironment webHostEnvironment = null) : IHtmlToPdfConverterService, ITransientService
{
    /// <inheritdoc />
    public virtual Task<FileContentResult> ConvertHtmlStringToPdfAsync(HtmlToPdfRequestModel settings)
    {
        // This method should be implemented in the derived class.
        return Task.FromResult<FileContentResult>(null);
    }

    /// <inheritdoc />
    public virtual string EnsureCorrectFileName(string input)
    {
        var output = input;
        // Generate unique file name if none was supplied.
        if (String.IsNullOrWhiteSpace(output))
        {
            output = $"{Guid.NewGuid():N}.pdf";
        }

        // Make sure the file name has the correct extension. (This function will add the extension if it doesn't exist, or change it if it's wrong.)
        return Path.ChangeExtension(output.StripIllegalFilenameCharacters(), ".pdf");
    }

    /// <inheritdoc />
    public virtual async Task<HtmlToPdfRequestModel> GetHtmlToPdfSettingsAsync(ulong templateItemId, string languageCode = null, string contentPropertyName = null)
    {
        var pdfSettings = new HtmlToPdfRequestModel
        {
            ItemId = templateItemId,
            BackgroundPropertyName = await objectsService.FindSystemObjectByDomainNameAsync(HtmlTemplateConstants.CustomBackGroundPropertySettingName)
        };

        var query = $"SELECT `key`, CONCAT_WS('', `value`, `long_value`) AS value, language_code FROM {WiserTableNames.WiserItemDetail} WHERE item_id = ?templateItemId";
        databaseConnection.AddParameter("templateItemId", templateItemId);
        var dataTable = await databaseConnection.GetAsync(query);
        if (dataTable.Rows.Count <= 0)
        {
            return pdfSettings;
        }

        // Get values with correct language code.
        if (!String.IsNullOrWhiteSpace(languageCode))
        {
            foreach (DataRow dataRow in dataTable.Rows)
            {
                if (!String.Equals(languageCode, dataRow.Field<string>("language_code")))
                {
                    continue;
                }

                var key = dataRow.Field<string>("key");
                var value = dataRow.Field<string>("value");
                if (String.Equals(key, HtmlTemplateConstants.PdfDocumentOptionsPropertyName))
                {
                    pdfSettings.DocumentOptions = value;
                }
                else if (String.Equals(key, HtmlTemplateConstants.PdfHeaderPropertyName))
                {
                    pdfSettings.Header = value;
                }
                else if (String.Equals(key, HtmlTemplateConstants.PdfFooterPropertyName))
                {
                    pdfSettings.Footer = value;
                }
                else if (String.Equals(key, HtmlTemplateConstants.HtmlTemplatePropertyName) || (!String.IsNullOrWhiteSpace(contentPropertyName) && String.Equals(key, contentPropertyName)))
                {
                    pdfSettings.Html = value;
                }
                else if (String.Equals(key, HtmlTemplateConstants.PdfOrientationPropertyName) && Int32.TryParse(value, out var orientation))
                {
                    pdfSettings.Orientation = (PageOrientation) orientation;
                }
                else if (String.Equals(key, HtmlTemplateConstants.PdfFileNamePropertyName))
                {
                    pdfSettings.FileName = value;
                }
                else if (String.Equals(key, HtmlTemplateConstants.PdfBackgroundImagePropertyName))
                {
                    pdfSettings.BackgroundPropertyName = value;
                }
            }
        }

        // Fall back to default language or no language.
        foreach (DataRow dataRow in dataTable.Rows)
        {
            var key = dataRow.Field<string>("key");
            var value = dataRow.Field<string>("value");
            if (String.Equals(key, HtmlTemplateConstants.PdfDocumentOptionsPropertyName) && String.IsNullOrWhiteSpace(pdfSettings.DocumentOptions))
            {
                pdfSettings.DocumentOptions = value;
            }
            else if (String.Equals(key, HtmlTemplateConstants.PdfHeaderPropertyName) && String.IsNullOrWhiteSpace(pdfSettings.Header))
            {
                pdfSettings.Header = value;
            }
            else if (String.Equals(key, HtmlTemplateConstants.PdfFooterPropertyName) && String.IsNullOrWhiteSpace(pdfSettings.Footer))
            {
                pdfSettings.Footer = value;
            }
            else if ((String.Equals(key, HtmlTemplateConstants.HtmlTemplatePropertyName) || (!String.IsNullOrWhiteSpace(contentPropertyName) && String.Equals(key, contentPropertyName))) && String.IsNullOrWhiteSpace(pdfSettings.Html))
            {
                pdfSettings.Html = value;
            }
            else if (String.Equals(key, HtmlTemplateConstants.PdfOrientationPropertyName) && Int32.TryParse(value, out var orientation) && !pdfSettings.Orientation.HasValue)
            {
                pdfSettings.Orientation = (PageOrientation) orientation;
            }
            else if (String.Equals(key, HtmlTemplateConstants.PdfFileNamePropertyName) && String.IsNullOrWhiteSpace(pdfSettings.FileName))
            {
                pdfSettings.FileName = value;
            }
            else if (String.Equals(key, HtmlTemplateConstants.PdfBackgroundImagePropertyName) && String.IsNullOrWhiteSpace(pdfSettings.BackgroundPropertyName))
            {
                pdfSettings.BackgroundPropertyName = value;
            }
        }

        return pdfSettings;
    }

    /// <inheritdoc />
    public virtual async Task<string> RetrieveBackgroundImageAsync(ulong itemId, string backgroundPropertyName)
    {
        if (webHostEnvironment == null)
        {
            return null;
        }

        databaseConnection.AddParameter("itemId", itemId);
        databaseConnection.AddParameter("propertyName", backgroundPropertyName);
        var getImageResult = await databaseConnection.GetAsync($"""
                                                                                SELECT content, file_name
                                                                                FROM `{WiserTableNames.WiserItemFile}`
                                                                                WHERE item_id = ?itemId 
                                                                                AND property_name = ?propertyName
                                                                                ORDER BY id
                                                                                LIMIT 1
                                                                """);

        // Return null if there's no row.
        if (getImageResult.Rows.Count == 0)
        {
            return null;
        }

        // Return null if the result is null or has no bytes.
        var content = getImageResult.Rows[0].Field<byte[]>("content");
        var filename = getImageResult.Rows[0].Field<string>("file_name");
        if (content == null || content.Length == 0)
        {
            return null;
        }

        var filePath = await FileSystemHelpers.SaveFileToCacheAsync(webHostEnvironment, filename, content);
        // ReSharper disable once ConstantConditionalAccessQualifier
        return filePath?.Replace(webHostEnvironment.WebRootPath, "")?.Replace(@"\", "/");
    }
}