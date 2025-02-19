using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using EvoPdf.Chromium;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.GclConverters.Services;

public class HtmlToPdfConverterService : IHtmlToPdfConverterService, IScopedService
{
    private readonly GclSettings gclSettings;
    private readonly IDatabaseConnection databaseConnection;
    private readonly IObjectsService objectsService;
    private readonly IStringReplacementsService stringReplacementsService;
    private readonly IHttpContextAccessor httpContextAccessor;
    private readonly IWebHostEnvironment webHostEnvironment;

    public HtmlToPdfConverterService(IDatabaseConnection databaseConnection,
        IObjectsService objectsService,
        IStringReplacementsService stringReplacementsService,
        IOptions<GclSettings> gclSettings,
        IHttpContextAccessor httpContextAccessor = null,
        IWebHostEnvironment webHostEnvironment = null)
    {
        this.databaseConnection = databaseConnection;
        this.objectsService = objectsService;
        this.stringReplacementsService = stringReplacementsService;
        this.httpContextAccessor = httpContextAccessor;
        this.webHostEnvironment = webHostEnvironment;
        this.gclSettings = gclSettings.Value;

        // Check if EvoPdf is loaded, otherwise throw an exception.
        // We load Evo PDF with PrivateAssets = true, so it won't be automatically loaded in projects that use the GCL.
        // This is because Evo PDF has separate packages for Windows and Linux, and we can't properly detect which one they need from here.
        var evoPdfType = Type.GetType("EvoPdf.Chromium.HtmlToPdfConverter, EvoPdf.Chromium");
        if (evoPdfType == null)
        {
            throw new InvalidOperationException("EvoPdf is not loaded. Please ensure you have added the correct NuGet package: Either 'EvoPdf.Chromium.Windows' for Windows or 'EvoPdf.Chromium.Linux' for Linux.");
        }
    }

    /// <inheritdoc />
    public async Task<FileContentResult> ConvertHtmlStringToPdfAsync(HtmlToPdfRequestModel settings)
    {
        var htmlToConvert = new StringBuilder(settings.Html);
        var httpContext = httpContextAccessor?.HttpContext;
        var converter = new HtmlToPdfConverter
        {
            LicenseKey = gclSettings.EvoPdfLicenseKey,
            ConversionDelay = 2
        };

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            // For some reason the default path doesn't work on Linux.
            // Note: This also requires the command "chmod +x /app/evopdf_loadhtml" to be run on the server.
            converter.HtmlLoaderFilePath = "/app/evopdf_loadhtml";
        }

        if (!settings.Orientation.HasValue)
        {
            var orientationValue = await objectsService.FindSystemObjectByDomainNameAsync("pdf_orientation");
            settings.Orientation = orientationValue.Equals("landscape", StringComparison.OrdinalIgnoreCase) ? PdfPageOrientation.Landscape : PdfPageOrientation.Portrait;
        }

        converter.PdfDocumentOptions.PdfPageOrientation = settings.Orientation.Value;

        Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_html_viewer_width"), out var htmlViewerWidth);
        Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_html_viewer_height"), out var htmlViewerHeight);
        Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_margins"), out var margins);

        // Main document options.
        var avoidTextBreak = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_avoid_text_break")).Equals("true", StringComparison.OrdinalIgnoreCase);
        var avoidImageBreak = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_avoid_image_break", "true")).Equals("true", StringComparison.OrdinalIgnoreCase);
        htmlToConvert.Insert(0, $$"""
                                  <style>
                                  	* {
                                  		break-inside: {{(avoidTextBreak ? "avoid" : "auto")}};
                                  	}
                                  	
                                  	img {
                                          break-inside: {{(avoidImageBreak ? "avoid" : "auto")}};
                                  	}
                                  </style>
                                  """);

        converter.PdfDocumentOptions.BottomMargin = margins;
        converter.PdfDocumentOptions.LeftMargin = margins;
        converter.PdfDocumentOptions.RightMargin = margins;
        converter.PdfDocumentOptions.TopMargin = margins;

        // Page size.
        var pageSize = await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize", "A4");
        if (pageSize == "CUSTOM")
        {
            Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize_width"), out var pageSizeWidth);
            Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize_height"), out var pageSizeHeight);

            converter.PdfDocumentOptions.PdfPageSize = new PdfPageSize(pageSizeWidth, pageSizeHeight);
        }
        else
        {
            converter.PdfDocumentOptions.AutoResizePdfPageWidth = false;
            converter.PdfDocumentOptions.PdfPageSize = pageSize switch
            {
                "A0" => PdfPageSize.A0,
                "A1" => PdfPageSize.A1,
                "A2" => PdfPageSize.A2,
                "A3" => PdfPageSize.A3,
                "A4" => PdfPageSize.A4,
                "A5" => PdfPageSize.A5,
                "A6" => PdfPageSize.A6,
                "A7" => PdfPageSize.A7,
                "A8" => PdfPageSize.A8,
                "A9" => PdfPageSize.A9,
                "A10" => PdfPageSize.A10,
                _ => PdfPageSize.A4
            };
        }

        if (htmlViewerWidth > 0) converter.HtmlViewerWidth = htmlViewerWidth;
        if (htmlViewerHeight > 0) converter.HtmlViewerHeight = htmlViewerHeight;

        // Header settings.
        converter.PdfDocumentOptions.EnableHeaderFooter = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_header_show")).Equals("true", StringComparison.OrdinalIgnoreCase) || (await objectsService.FindSystemObjectByDomainNameAsync("pdf_footer_show")).Equals("true", StringComparison.OrdinalIgnoreCase);
        if (String.IsNullOrWhiteSpace(settings.Header))
        {
            settings.Header = await objectsService.FindSystemObjectByDomainNameAsync("pdf_header_text");
        }

        if (!String.IsNullOrWhiteSpace(settings.Header))
        {
            converter.PdfDocumentOptions.HeaderTemplate = settings.Header;
        }

        // Footer settings.
        if (String.IsNullOrWhiteSpace(settings.Footer))
        {
            settings.Footer = await objectsService.FindSystemObjectByDomainNameAsync("pdf_footer_text");
        }

        if (!String.IsNullOrWhiteSpace(settings.Footer))
        {
            converter.PdfDocumentOptions.FooterTemplate = settings.Footer;
        }

        // Security settings.
        converter.PdfSecurityOptions.CanEditContent = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_can_edit_content", "false")).Equals("true", StringComparison.OrdinalIgnoreCase);
        converter.PdfSecurityOptions.CanCopyContent = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_can_copy_content", "true")).Equals("true", StringComparison.OrdinalIgnoreCase);
        converter.PdfSecurityOptions.OwnerPassword = await objectsService.FindSystemObjectByDomainNameAsync("pdf_password");

        if (String.IsNullOrWhiteSpace(converter.PdfSecurityOptions.OwnerPassword))
        {
            converter.PdfSecurityOptions.OwnerPassword = SecurityHelpers.GenerateRandomPassword();
        }
        else
        {
            converter.PdfSecurityOptions.OwnerPassword = await stringReplacementsService.DoAllReplacementsAsync(converter.PdfSecurityOptions.OwnerPassword);
        }

        if (settings.ItemId > 0UL && !String.IsNullOrWhiteSpace(settings.BackgroundPropertyName))
        {
            var backgroundImage = await RetrieveBackgroundImageAsync(settings.ItemId, settings.BackgroundPropertyName);

            if (!String.IsNullOrWhiteSpace(backgroundImage))
            {
                htmlToConvert.Insert(0, $$"""
                                          <style>
                                            html {
                                                width: 100%;
                                                height: 100%;
                                                margin: 0;
                                                padding: 0;
                                            }
                                            
                                          	body {
                                                width: 100%;
                                                height: 100%;
                                          		margin: 0;
                                          		padding: 0;
                                          		background-image: url('{{backgroundImage}}');
                                          		background-size: cover; /* Ensure the image covers the full page */
                                          		background-repeat: repeat-y;
                                          		background-position: top left;
                                          	}
                                          </style>
                                          """);
            }
        }

        // Set additional document options.
        var options = String.IsNullOrWhiteSpace(settings.DocumentOptions) ? new Dictionary<string, string>() : settings.DocumentOptions.Split(';').Where(o => o.Contains(":")).ToDictionary(o => o.Split(':')[0], o => o.Split(':')[1], StringComparer.OrdinalIgnoreCase);

        foreach (var p in typeof(HtmlToPdfConverter).GetProperties())
        {
            if (!options.ContainsKey(p.Name))
            {
                continue;
            }

            p.SetValue(converter, Convert.ChangeType(options[p.Name], p.PropertyType), null);
        }

        foreach (var p in typeof(PdfDocumentOptions).GetProperties())
        {
            if (!options.ContainsKey(p.Name))
            {
                continue;
            }

            p.SetValue(converter.PdfDocumentOptions, Convert.ChangeType(options[p.Name], p.PropertyType), null);
        }

        var baseUri = httpContext == null ? "/" : HttpContextHelpers.GetBaseUri(httpContext).ToString();
        var output = converter.ConvertHtml(htmlToConvert.ToString(), baseUri);
        var fileResult = new FileContentResult(output, "application/pdf")
        {
            FileDownloadName = EnsureCorrectFileName(settings.FileName)
        };

        return fileResult;
    }

    /// <inheritdoc />
    public string EnsureCorrectFileName(string input)
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
    public async Task<HtmlToPdfRequestModel> GetHtmlToPdfSettingsAsync(ulong templateItemId, string languageCode = null, string contentPropertyName = null)
    {
        var pdfSettings = new HtmlToPdfRequestModel
        {
            ItemId = templateItemId
        };

        pdfSettings.BackgroundPropertyName = await objectsService.FindSystemObjectByDomainNameAsync("pdf_backgroundpropertyname");

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
                    pdfSettings.Orientation = (PdfPageOrientation) orientation;
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
                pdfSettings.Orientation = (PdfPageOrientation) orientation;
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

    private async Task<string> RetrieveBackgroundImageAsync(ulong itemId, string backgroundPropertyName)
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
                                                                                WHERE item_id = ?itemId AND property_name = ?propertyName
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

        var filePath = await FileSystemHelpers.SaveToPublicFilesDirectoryAsync(webHostEnvironment, filename, content);
        return filePath?.Replace(webHostEnvironment.WebRootPath, "")?.Replace(@"\", "/");
    }
}