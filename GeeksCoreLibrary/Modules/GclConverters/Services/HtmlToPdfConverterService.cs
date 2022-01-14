using EvoPdf;
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
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.GclConverters.Services
{
    public class HtmlToPdfConverterService : IHtmlToPdfConverterService, IScopedService
    {
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IObjectsService objectsService;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly ILogger<HtmlToPdfConverterService> logger;
        private readonly GclSettings gclSettings;

        public HtmlToPdfConverterService(IHttpContextAccessor httpContextAccessor, IDatabaseConnection databaseConnection, IObjectsService objectsService, IStringReplacementsService stringReplacementsService, IWebHostEnvironment webHostEnvironment, IOptions<GclSettings> gclSettings, ILogger<HtmlToPdfConverterService> logger)
        {
            this.httpContextAccessor = httpContextAccessor;
            this.databaseConnection = databaseConnection;
            this.objectsService = objectsService;
            this.stringReplacementsService = stringReplacementsService;
            this.webHostEnvironment = webHostEnvironment;
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
        }

        /// <inheritdoc />
        public async Task<FileContentResult> ConvertHtmlStringToPdfAsync(HtmlToPdfRequestModel settings)
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("No http context available.");
            }

            var converter = new HtmlToPdfConverter
            {
                LicenseKey = gclSettings.EvoPdfLicenseKey
            };

            if (!settings.Orientation.HasValue)
            {
                var orientationValue = await objectsService.FindSystemObjectByDomainNameAsync("pdf_orientation");
                settings.Orientation = orientationValue.Equals("landscape", StringComparison.OrdinalIgnoreCase) ? PdfPageOrientation.Landscape : PdfPageOrientation.Portrait;
            }

            converter.PdfDocumentOptions.PdfPageOrientation = settings.Orientation.Value;

            Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_html_viewer_width"), out var htmlViewerWidth);
            Int32.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_html_viewer_height"), out var htmlViewerHeight);
            Single.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_margins"), out var margins);

            // Main document options.
            converter.PdfDocumentOptions.FitHeight = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_fit_height")).Equals("true", StringComparison.OrdinalIgnoreCase);
            converter.PdfDocumentOptions.AvoidImageBreak = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_avoid_image_break", "true")).Equals("true", StringComparison.OrdinalIgnoreCase);
            converter.PdfDocumentOptions.AvoidTextBreak = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_avoid_text_break")).Equals("true", StringComparison.OrdinalIgnoreCase);
            converter.PdfDocumentOptions.EmbedFonts = true;
            converter.PdfDocumentOptions.BottomMargin = margins;
            converter.PdfDocumentOptions.LeftMargin = margins;
            converter.PdfDocumentOptions.RightMargin = margins;
            converter.PdfDocumentOptions.TopMargin = margins;
            converter.PdfDocumentOptions.PdfCompressionLevel = PdfCompressionLevel.Best;

            // Page size.
            var pageSize = await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize", "A4");
            if (pageSize == "CUSTOM")
            {
                Single.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize_width"), out var pageSizeWidth);
                Single.TryParse(await objectsService.FindSystemObjectByDomainNameAsync("pdf_pagesize_height"), out var pageSizeHeight);

                converter.PdfDocumentOptions.PdfPageSize = new PdfPageSize(pageSizeWidth, pageSizeHeight);
                converter.PdfDocumentOptions.AutoSizePdfPage = true;
            }
            else
            {
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
            converter.PdfDocumentOptions.ShowHeader = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_header_show")).Equals("true", StringComparison.OrdinalIgnoreCase);
            if (String.IsNullOrWhiteSpace(settings.Header))
            {
                settings.Header = await objectsService.FindSystemObjectByDomainNameAsync("pdf_header_text");
            }
            if (!String.IsNullOrWhiteSpace(settings.Header))
            {
                var headerElem = new HtmlToPdfElement(settings.Header, null) { FitHeight = true };
                headerElem.NavigationCompletedEvent += delegate(NavigationCompletedParams eventParams)
                {
                    var headerHtmlWidth = eventParams.HtmlContentWidthPt;
                    var headerHtmlHeight = eventParams.HtmlContentHeightPt;
                    var headerWidth = converter.PdfDocumentOptions.PdfPageSize.Width - converter.PdfDocumentOptions.LeftMargin - converter.PdfDocumentOptions.RightMargin;
                    float resizeFactor = 1;
                    if (headerHtmlWidth > headerWidth)
                    {
                        resizeFactor = headerWidth / headerHtmlWidth;
                    }

                    var headerHeight = headerHtmlHeight * resizeFactor;

                    if (!(headerHeight < converter.PdfDocumentOptions.PdfPageSize.Height - converter.PdfDocumentOptions.TopMargin - converter.PdfDocumentOptions.BottomMargin))
                    {
                        throw new Exception("The header height cannot be bigger than PDF page height");
                    }

                    converter.PdfDocumentOptions.DocumentObject.Header.Height = headerHeight;
                };
                converter.PdfHeaderOptions.AddElement(headerElem);
            }

            // Footer settings.
            converter.PdfDocumentOptions.ShowFooter = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_footer_show")).Equals("true", StringComparison.OrdinalIgnoreCase);
            if (String.IsNullOrWhiteSpace(settings.Footer))
            {
                settings.Header = await objectsService.FindSystemObjectByDomainNameAsync("pdf_footer_text");
            }
            if (!String.IsNullOrWhiteSpace(settings.Footer))
            {
                var footerElem = new HtmlToPdfElement(settings.Footer, null) { FitHeight = true };
                footerElem.NavigationCompletedEvent += delegate(NavigationCompletedParams eventParams)
                {
                    var footerHtmlWidth = eventParams.HtmlContentWidthPt;
                    var footerHtmlHeight = eventParams.HtmlContentHeightPt;
                    var footerWidth = converter.PdfDocumentOptions.PdfPageSize.Width - converter.PdfDocumentOptions.LeftMargin - converter.PdfDocumentOptions.RightMargin;
                    float resizeFactor = 1;
                    if (footerHtmlWidth > footerWidth)
                    {
                        resizeFactor = footerWidth / footerHtmlWidth;
                    }

                    var footerHeight = footerHtmlHeight * resizeFactor;

                    if (!(footerHeight < converter.PdfDocumentOptions.PdfPageSize.Height - converter.PdfDocumentOptions.TopMargin - converter.PdfDocumentOptions.BottomMargin))
                    {
                        throw new Exception("The footer height cannot be bigger than PDF page height");
                    }

                    converter.PdfDocumentOptions.DocumentObject.Footer.Height = footerHeight;
                };
                converter.PdfFooterOptions.AddElement(footerElem);
            }

            // Security settings.
            converter.PdfSecurityOptions.CanEditContent = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_can_edit_content")).Equals("true", StringComparison.OrdinalIgnoreCase);
            converter.PdfSecurityOptions.CanCopyContent = (await objectsService.FindSystemObjectByDomainNameAsync("pdf_can_copy_content")).Equals("true", StringComparison.OrdinalIgnoreCase);
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
                    try
                    {
                        converter.BeforeRenderPdfPageEvent += parameters =>
                        {
                            var pdfPage = parameters.Page;
                            var pdfPageWidth = pdfPage.ClientRectangle.Width;
                            var pdfPageHeight = pdfPage.ClientRectangle.Height;

                            var backgroundElement = new ImageElement(0, 0, pdfPageWidth, pdfPageHeight, backgroundImage)
                            {
                                KeepAspectRatio = true,
                                EnlargeEnabled = true
                            };
                            pdfPage.AddElement(backgroundElement);

                        };
                    }
                    catch (Exception exception)
                    {
                        logger.LogWarning(exception, "An error occurred while adding a background to a PDF in ConvertHtmlStringToPdfAsync()");
                    }
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

            var output = converter.ConvertHtml(settings.Html, HttpContextHelpers.GetBaseUri(httpContext).ToString());
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

        private async Task<string> RetrieveBackgroundImageAsync(ulong itemId, string backgroundPropertyName)
        {
            databaseConnection.AddParameter("itemId", itemId);
            databaseConnection.AddParameter("propertyName", backgroundPropertyName);
            var getImageResult = await databaseConnection.GetAsync($@"
                SELECT content, file_name
                FROM `{WiserTableNames.WiserItemFile}`
                WHERE item_id = ?itemId AND property_name = ?propertyName
                ORDER BY id
                LIMIT 1");

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

            return FileSystemHelpers.SaveFileToContentFilesFolder(webHostEnvironment, filename, content);
        }
    }
}
