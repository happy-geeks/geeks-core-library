using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Extensions;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Data;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Filter.Interfaces;
using EvoPdf;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Newtonsoft.Json.Linq;
using Template = GeeksCoreLibrary.Modules.Templates.Models.Template;

namespace GeeksCoreLibrary.Modules.Templates.Services
{
    /// <summary>
    /// This class provides template caching, template replacements and rendering
    /// for all types of templates, like CSS, JS, Query's and HTML templates.
    /// </summary>
    public class TemplatesService : ITemplatesService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly ILogger<TemplatesService> logger;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IStringReplacementsService stringReplacementsService;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IViewComponentHelper viewComponentHelper;
        private readonly ITempDataProvider tempDataProvider;
        private readonly IActionContextAccessor actionContextAccessor;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IObjectsService objectService;
        private readonly IFiltersService filtersService;


        /// <summary>
        /// Initializes a new instance of <see cref="TemplatesService"/>.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="gclSettings"></param>
        /// <param name="databaseConnection"></param>
        /// <param name="stringReplacementsService"></param>
        /// <param name="httpContextAccessor"></param>
        /// <param name="viewComponentHelper"></param>
        /// <param name="tempDataProvider"></param>
        /// <param name="actionContextAccessor"></param>
        /// <param name="webHostEnvironment"></param>
        /// <param name="filtersService"></param>
        public TemplatesService(ILogger<TemplatesService> logger,
            IOptions<GclSettings> gclSettings,
            IDatabaseConnection databaseConnection,
            IStringReplacementsService stringReplacementsService,
            IHttpContextAccessor httpContextAccessor,
            IViewComponentHelper viewComponentHelper,
            ITempDataProvider tempDataProvider,
            IActionContextAccessor actionContextAccessor,
            IWebHostEnvironment webHostEnvironment,
            IFiltersService filtersService,
            IObjectsService objectService)
        {
            this.gclSettings = gclSettings.Value;
            this.logger = logger;
            this.databaseConnection = databaseConnection;
            this.stringReplacementsService = stringReplacementsService;
            this.httpContextAccessor = httpContextAccessor;
            this.viewComponentHelper = viewComponentHelper;
            this.tempDataProvider = tempDataProvider;
            this.actionContextAccessor = actionContextAccessor;
            this.webHostEnvironment = webHostEnvironment;
            this.filtersService = filtersService;
            this.objectService = objectService;
        }

        /// <inheritdoc />
        public async Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes type = TemplateTypes.Html, int parentId = 0, string parentName = "")
        {
            if (id <= 0 && String.IsNullOrEmpty(name))
            {
                throw new ArgumentNullException($"One of the parameters {nameof(id)} or {nameof(name)} must contain a value");
            }
            
            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new NotImplementedException($"Unknown environment '{gclSettings.Environment}'!"),
            };

            string whereClause;
            if (id > 0)
            {
                databaseConnection.AddParameter("id", id);
                whereClause = "i.id = ?id";
            }
            else
            {
                databaseConnection.AddParameter("name", name);
                whereClause = "i.name = ?name";
            }

            if (parentId > 0)
            {
                databaseConnection.AddParameter("parentId", parentId);
                whereClause += " AND ip.id = ?parentIId";
            }
            else if (!String.IsNullOrWhiteSpace(parentName))
            {
                databaseConnection.AddParameter("parentName", parentName);
                whereClause = " AND ip.name = ?parentName";
            }

            var query = $@"SELECT
                            IFNULL(ippppp.name, IFNULL(ipppp.name, IFNULL(ippp.name, IFNULL(ipp.name, ip.name)))) as rootName, 
                            ip.`name` AS parentName, 
                            ip.id AS parentId,
                            i.`name`,
                            t.templatetype,
                            i.volgnr,
                            ip.volgnr AS parentOrder,
                            i.id AS templateId,
                            t.csstemplates,
                            t.jstemplates,
                            t.loadalways,
                            t.lastchanged,
                            t.externalfiles,
                            t.html_obfuscated,
                            t.html_minified,
                            t.html,
                            t.template,
                            t.urlregex,
                            t.usecache,
                            t.cacheminutes,
                            t.useobfuscate,
                            t.defaulttemplate,
                            t.pagemode,
                            t.groupingCreateObjectInsteadOfArray,
                            t.groupingKeyColumnName,
                            t.groupingValueColumnName,
                            t.groupingkey,
                            t.groupingprefix
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id=t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND {whereClause}
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            await using var reader = await databaseConnection.GetReaderAsync(query);
            var result = await reader.ReadAsync() ? await reader.ToTemplateModelAsync(type) : new Template();

            return result;
        }

        /// <inheritdoc />
        public async Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType)
        {
            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new NotImplementedException($"Unknown environment '{gclSettings.Environment}'!")
            };

            var query = $@"SELECT MAX(t.lastchanged) AS lastChanged
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        AND t.templatetype = ?templateType
                        ORDER BY t.lastchanged";

            databaseConnection.AddParameter("templateType", templateType.ToString());
            DateTime? result;
            await using var reader = await databaseConnection.GetReaderAsync(query);
            if (!await reader.ReadAsync())
            {
                return null;
            }

            var ordinal = reader.GetOrdinal("lastChanged");
            result = await reader.IsDBNullAsync(ordinal) ? null : reader.GetDateTime(ordinal);
            return result;
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType)
        {
            databaseConnection.AddParameter("templateType", templateType.ToString());

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new NotImplementedException($"Unknown environment '{gclSettings.Environment}'!")
            };

            var query = $@"SELECT
                            IFNULL(ippppp.name, IFNULL(ipppp.name, IFNULL(ippp.name, IFNULL(ipp.name, ip.name)))) as rootName, 
                            ip.`name` AS parentName, 
                            ip.id AS parentId,
                            i.`name`,
                            t.templatetype,
                            i.volgnr,
                            ip.volgnr AS parentOrder,
                            i.id AS templateId,
                            t.csstemplates,
                            t.jstemplates,
                            t.loadalways,
                            t.lastchanged,
                            t.externalfiles,
                            t.html_obfuscated,
                            t.html_minified,
                            t.html,
                            t.template,
                            t.urlregex,
                            t.usecache,
                            t.cacheminutes,
                            t.useobfuscate,
                            t.defaulttemplate,
                            t.pagemode,
                            t.groupingCreateObjectInsteadOfArray,
                            t.groupingKeyColumnName,
                            t.groupingValueColumnName,
                            t.groupingkey,
                            t.groupingprefix
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        AND t.templatetype = ?templateType
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();

            await using var reader = await databaseConnection.GetReaderAsync(query);
            while (await reader.ReadAsync())
            {
                var template = await reader.ToTemplateModelAsync();
                await AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
            }

            result.Content = resultBuilder.ToString();

            if (result.LastChangeDate == DateTime.MinValue)
            {
                result.LastChangeDate = DateTime.Now;
            }

            if (templateType == TemplateTypes.Css)
            {
                result.Content = CssHelpers.MoveImportStatementsToTop(result.Content);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent)
        {
            var results = new List<Template>();
            databaseConnection.AddParameter("includeContent", includeContent);

            var joinPart = gclSettings.Environment switch
            {
                Environments.Development => " JOIN (SELECT itemid, max(version) AS maxversion FROM easy_templates GROUP BY itemid) v ON t.itemid = v.itemid AND t.version = v.maxversion ",
                Environments.Test => " AND t.istest=1 ",
                Environments.Acceptance => " AND t.isacceptance=1 ",
                Environments.Live => " AND t.islive=1 ",
                _ => throw new NotImplementedException($"Unknown environment '{gclSettings.Environment}'!")
            };

            var query = $@"SELECT
                            IFNULL(ippppp.name, IFNULL(ipppp.name, IFNULL(ippp.name, IFNULL(ipp.name, ip.name)))) as rootName, 
                            ip.`name` AS parentName, 
                            ip.id AS parentId,
                            i.`name`,
                            t.templatetype,
                            i.volgnr,
                            ip.volgnr AS parentOrder,
                            i.id AS templateId,
                            t.csstemplates,
                            t.jstemplates,
                            t.loadalways,
                            t.lastchanged,
                            t.externalfiles,
                            IF(?includeContent, t.html_obfuscated, '') AS html_obfuscated,
                            IF(?includeContent, t.html_minified, '') AS html_minified,
                            IF(?includeContent, t.html, '') AS html,
                            IF(?includeContent, t.template, '') AS template,
                            t.urlregex,
                            t.usecache,
                            t.cacheminutes,
                            t.useobfuscate,
                            t.defaulttemplate,
                            t.pagemode,
                            t.groupingCreateObjectInsteadOfArray,
                            t.groupingKeyColumnName,
                            t.groupingValueColumnName,
                            t.groupingkey,
                            t.groupingprefix
                        FROM easy_items i 
                        JOIN easy_templates t ON i.id = t.itemid
                        {joinPart}
                        LEFT JOIN easy_items ip ON i.parent_id = ip.id
                        LEFT JOIN easy_items ipp ON ip.parent_id = ipp.id
                        LEFT JOIN easy_items ippp ON ipp.parent_id = ippp.id
                        LEFT JOIN easy_items ipppp ON ippp.parent_id = ipppp.id
                        LEFT JOIN easy_items ippppp ON ipppp.parent_id = ippppp.id
                        WHERE i.id IN ({String.Join(",", templateIds)})
                        AND i.moduleid = 143 
                        AND i.published = 1
                        AND i.deleted <= 0
                        AND t.deleted <= 0
                        AND t.loadalways > 0
                        ORDER BY ippppp.volgnr, ipppp.volgnr, ippp.volgnr, ipp.volgnr, ip.volgnr, i.volgnr";

            await using var reader = await databaseConnection.GetReaderAsync(query);
            while (await reader.ReadAsync())
            {
                var template = await reader.ToTemplateModelAsync();
                results.Add(template);
            }

            return results;
        }

        /// <inheritdoc />
        public async Task<TemplateResponse> GetCombinedTemplateValueAsync(ICollection<int> templateIds, TemplateTypes templateType)
        {
            var result = new TemplateResponse();
            var resultBuilder = new StringBuilder();
            var idsLoaded = new List<int>();
            var currentUrl = HttpContextHelpers.GetOriginalRequestUri(httpContextAccessor.HttpContext).ToString();
            var templates = await GetTemplatesAsync(templateIds, true);

            foreach (var template in templates.Where(t => t.Type == templateType))
            {
                await AddTemplateToResponseAsync(idsLoaded, template, currentUrl, resultBuilder, result);
            }

            result.Content = resultBuilder.ToString();

            if (result.LastChangeDate == DateTime.MinValue)
            {
                result.LastChangeDate = DateTime.Now;
            }

            if (templateType == TemplateTypes.Css)
            {
                result.Content = CssHelpers.MoveImportStatementsToTop(result.Content);
            }

            return result;
        }

        /// <inheritdoc />
        public async Task AddTemplateToResponseAsync(ICollection<int> idsLoaded, Template template, string currentUrl, StringBuilder resultBuilder, TemplateResponse templateResponse)
        {
            if (idsLoaded.Contains(template.Id))
            {
                // Make sure that we don't add the same template twice.
                return;
            }

            if (!String.IsNullOrWhiteSpace(template.UrlRegex) && !Regex.IsMatch(currentUrl, template.UrlRegex))
            {
                // Skip this template if it has an URL regex and that regex does not match the current URL.
                return;
            }

            idsLoaded.Add(template.Id);

            // Get files from Wiser CDN.
            if (template.WiserCdnFiles.Any())
            {
                resultBuilder.AppendLine(await GetWiserCdnFilesAsync(template.WiserCdnFiles));
            }

            // Get the template contents.
            resultBuilder.AppendLine(template.Content);

            // Get the change date.
            if (template.LastChanged > templateResponse.LastChangeDate)
            {
                templateResponse.LastChangeDate = template.LastChanged;
            }

            // Get any external files that we need to load.
            templateResponse.ExternalFiles.AddRange(template.ExternalFiles);
        }

        /// <inheritdoc />
        public async Task<string> GetWiserCdnFilesAsync(ICollection<string> fileNames)
        {
            if (fileNames == null)
            {
                throw new ArgumentNullException(nameof(fileNames));
            }

            var enumerable = fileNames.ToList();
            if (!enumerable.Any())
            {
                return "";
            }

            var resultBuilder = new StringBuilder();
            using var webClient = new WebClient();
            foreach (var fileName in enumerable.Where(fileName => !String.IsNullOrWhiteSpace(fileName)))
            {
                var extension = Path.GetExtension(fileName).ToLowerInvariant();
                var directory = extension switch
                {
                    ".js" => "scripts",
                    _ => extension.Substring(1)
                };

                var localDirectory = Path.Combine(webHostEnvironment.WebRootPath, directory);
                if (!Directory.Exists(localDirectory))
                {
                    Directory.CreateDirectory(localDirectory);
                }

                var fileLocation = Path.Combine(localDirectory, fileName);
                if (!File.Exists(fileLocation))
                {
                    await webClient.DownloadFileTaskAsync(new Uri($"https://app.wiser.nl/{directory}/cdn/{fileName}"), fileLocation);
                }

                resultBuilder.AppendLine(await File.ReadAllTextAsync(fileLocation));
            }

            return resultBuilder.ToString();
        }

        /// <inheritdoc />
        public async Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false)
        {
            // Input cannot be empty.
            if (String.IsNullOrEmpty(input))
            {
                return input;
            }

            // Start with normal string replacements, because includes can contain variables in a query string, which need to be replaced first.
            if (handleStringReplacements)
            {
                input = await stringReplacementsService.DoAllReplacementsAsync(input, dataRow, handleRequest, false, removeUnknownVariables, forQuery);
            }

            // HTML and mail templates.
            // Note: The string replacements service cannot handle the replacing of templates, because that would cause the StringReplacementsService to need
            // the TemplatesService, which in turn needs the StringReplacementsService, creating a circular dependency.
            input = await HandleIncludesAsync(input, forQuery: forQuery);
            input = await HandleImageTemplating(input);

            // Replace dynamic content.
            if (handleDynamicContent && !forQuery)
            {
                input = await ReplaceAllDynamicContentAsync(input);
            }

            if (evaluateLogicSnippets)
            {
                input = stringReplacementsService.EvaluateTemplate(input);
            }

            return input;
        }

        public async Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "")
        {
            var imageUrlTemplate = await objectService.FindSystemObjectByDomainNameAsync("image_url_template", "/image/wiser2/<item_id>/<type>/<resizemode>/<width>/<height>/<number>/<filename>");

            imageUrlTemplate = imageUrlTemplate.Replace("<item_id>", itemId);
            imageUrlTemplate = imageUrlTemplate.Replace("<filename>", filename);
            imageUrlTemplate = imageUrlTemplate.Replace("<type>", type);
            imageUrlTemplate = imageUrlTemplate.Replace("<width>", width);
            imageUrlTemplate = imageUrlTemplate.Replace("<height>", height);

            // Remove if not specified
            if (number == 0)
            {
                imageUrlTemplate = imageUrlTemplate.Replace("<number>/", "");
            }

            // Remove if not specified
            if (string.IsNullOrWhiteSpace(resizeMode))
            {
                imageUrlTemplate = imageUrlTemplate.Replace("<resizemode>/", "");
            }

            imageUrlTemplate = imageUrlTemplate.Replace("<number>", number.ToString());
            imageUrlTemplate = imageUrlTemplate.Replace("<resizemode>", resizeMode);

            return imageUrlTemplate;
        }

        public async Task<string> HandleImageTemplating(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            var imageTemplatingRegex = new Regex(@"\[image\[(.*?)\]\]");
            foreach (Match m in imageTemplatingRegex.Matches(input))
            {
                var replacementParameters = m.Groups[1].Value.Split(":");
                var outputBuilder = new StringBuilder();
                var imageIndex = 0;
                var resizeMode = "";
                var propertyName = "";
                var imageAltTag = "";
                var parameters = replacementParameters[0].Split(",");
                var imageItemIdOrFilename = parameters[0];

                // Only get the parameter if specified in the templating variable
                if (parameters.Length > 1)
                {
                    propertyName = parameters[1].Trim();
                }

                if (parameters.Length > 2)
                {
                    imageIndex = int.Parse(parameters[2].Trim());
                }

                if (parameters.Length > 3)
                {
                    resizeMode = parameters[3].Trim();
                }

                if (parameters.Length > 4)
                {
                    imageAltTag = parameters[4].Trim();
                }

                imageIndex = imageIndex == 0 ? 1 : imageIndex;
                 
                // Get the image from the database
                databaseConnection.AddParameter("itemId", imageItemIdOrFilename);
                databaseConnection.AddParameter("filename", imageItemIdOrFilename);
                databaseConnection.AddParameter("propertyName", propertyName);

                var queryWherePart = char.IsNumber(imageItemIdOrFilename, 0) ? "item_id = ?itemId" : "file_name = ?filename";
                var dataTable = await databaseConnection.GetAsync(@$"SELECT * FROM `{WiserTableNames.WiserItemFile}` WHERE {queryWherePart} AND IF(?propertyName = '', 1=1, property_name = ?propertyName) AND content_type LIKE 'image%' ORDER BY id ASC");

                if (dataTable.Rows.Count == 0)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, "image not found");
                    continue;
                }

                if (imageIndex > dataTable.Rows.Count)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, "specified image index out of bound");
                    continue;
                }

                // Get various values from the table
                var imageItemId = dataTable.Rows[imageIndex-1].Field<int>("item_id").ToString();
                var imageFilename = dataTable.Rows[imageIndex-1].Field<string>("file_name");
                var imagePropertyType = dataTable.Rows[imageIndex-1].Field<string>("property_name");
                var imageFilenameWithoutExt = Path.GetFileNameWithoutExtension(imageFilename);
                var imageTemplatingSetsRegex = new Regex(@"\:(.*?)\)");
                var items = imageTemplatingSetsRegex.Matches(m.Groups[1].Value);
                var totalItems = items.Count;
                var index = 1;

                if (items.Count == 0)
                {
                    input = input.ReplaceCaseInsensitive(m.Value, "no image set(s) specified, you must at least specify one set");
                    continue;
                }

                foreach (Match s in items)
                {
                    var imageTemplate = await objectService.FindSystemObjectByDomainNameAsync("image_template", "<figure><picture>{images}</picture></figure>");

                    // Get the specified parameters from the regex match
                    parameters = s.Value.Split(":")[1].Split("(");
                    var imageParameters = parameters[1].Replace(")", "").Split("x");
                    var imageViewportParameter = parameters[0];

                    if (string.IsNullOrWhiteSpace(imageViewportParameter))
                    {
                        input = input.ReplaceCaseInsensitive(m.Value, "no viewport parameter specified");
                        continue;
                    }

                    var imageWidth = Convert.ToInt32(imageParameters[0]);
                    var imageHeight = Convert.ToInt32(imageParameters[1]);
                    var imageWidth2X = (imageWidth * 2).ToString();
                    var imageHeight2X = (imageHeight * 2).ToString();

                    outputBuilder.AppendLine(@"<source media=""(min-width: {min-width}px)"" srcset=""{image-url-webp-2x} 2x, {image-url-webp}"" type=""image/webp"" />");
                    outputBuilder.AppendLine(@"<source media=""(min-width: {min-width}px)"" srcset=""{image-url-jpg-2x} 2x, {image-url-jpg}"" type=""image/jpeg"" />");

                    outputBuilder.Replace("{image-url-webp}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, imageFilenameWithoutExt + ".webp", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                    outputBuilder.Replace("{image-url-jpg}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, imageFilenameWithoutExt + ".jpg", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                    outputBuilder.Replace("{image-url-webp-2x}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, imageFilenameWithoutExt + ".webp", imageWidth2X, imageHeight2X, resizeMode));
                    outputBuilder.Replace("{image-url-jpg-2x}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, imageFilenameWithoutExt + ".jpg", imageWidth2X, imageHeight2X, resizeMode));
                    outputBuilder.Replace("{min-width}", imageViewportParameter);

                    // If last item, than add the default image
                    if (index == totalItems)
                    {
                        outputBuilder.AppendLine("<img width=\"100%\" height=\"auto\" loading=\"lazy\" src=\"{default_image_link}\" alt=\"{image_alt}\">");
                        outputBuilder.Replace("{default_image_link}", await GenerateImageUrl(imageItemId, imagePropertyType, imageIndex, imageFilenameWithoutExt + ".webp", imageWidth.ToString(), imageHeight.ToString(), resizeMode));
                    }

                    imageTemplate = imageTemplate.Replace("{images}", outputBuilder.ToString());
                    imageTemplate = imageTemplate.Replace("{image_alt}", (string.IsNullOrWhiteSpace(imageAltTag) ? imageFilename : imageAltTag));

                    // Replace the image in the template
                    input = input.ReplaceCaseInsensitive(m.Value, imageTemplate);

                    index += 1;
                }
            }

            return input;
        }

        /// <inheritdoc />
        public async Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false)
        {
            if (String.IsNullOrWhiteSpace(input))
            {
                return input;
            }

            const int max = 10;
            var counter = 0;

            // We use a while loop here because it's possible to to include a template that has another include, so we might have to replace them multiple times.
            while (counter < max && (input.Contains("<[", StringComparison.Ordinal) || input.Contains("[include", StringComparison.Ordinal)))
            {
                counter += 1;
                var inclusionsRegex = new Regex(@"<\[(.*?)\]>");
                foreach (Match m in inclusionsRegex.Matches(input))
                {
                    var templateName = m.Groups[1].Value;
                    if (templateName.Contains("{"))
                    {
                        // Make sure replaces for the template name are done
                        templateName = await stringReplacementsService.DoAllReplacementsAsync(templateName, dataRow, handleRequest, forQuery: forQuery);
                    }

                    // Replace templates (syntax is <[templateName]> or <[parentFolder\templateName]>
                    if (templateName.Contains("\\"))
                    {
                        // Contains a parent
                        var split = templateName.Split('\\');
                        var template = await GetTemplateAsync(name: split[1], parentName: split[0]);
                        if (handleStringReplacements)
                        {
                            template.Content = await stringReplacementsService.DoAllReplacementsAsync(template.Content, dataRow, handleRequest, false, false, forQuery);
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, template.Content);
                    }
                    else
                    {
                        var template = await GetTemplateAsync(name: templateName);
                        if (handleStringReplacements)
                        {
                            template.Content = await stringReplacementsService.DoAllReplacementsAsync(template.Content, dataRow, handleRequest, false, false, forQuery);
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, template.Content);
                    }
                }

                inclusionsRegex = new Regex(@"\[include\[([^{?\]]*)(\?)?([^{?\]]*?)\]\]");
                foreach (Match m in inclusionsRegex.Matches(input))
                {
                    var templateName = m.Groups[1].Value;
                    var queryString = m.Groups[3].Value.Replace("&amp;", "&");
                    if (templateName.Contains("{"))
                    {
                        // Make sure replaces for the template name are done
                        templateName = await stringReplacementsService.DoAllReplacementsAsync(templateName, dataRow, handleRequest, forQuery: forQuery);
                    }

                    // Replace templates (syntax is [include[templateName]] or [include[parentFolder\templateName]] or [include[templateName?x=y]]
                    if (templateName.Contains("\\"))
                    {
                        // Contains a parent
                        var split = templateName.Split('\\');
                        var template = await GetTemplateAsync(name: split[1], parentName: split[0]);
                        var values = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(x => new KeyValuePair<string, string>(x.Split('=')[0], x.Split('=')[1]));
                        var content = stringReplacementsService.DoReplacements(template.Content, values, forQuery);
                        if (handleStringReplacements)
                        {
                            content = await stringReplacementsService.DoAllReplacementsAsync(content, dataRow, handleRequest, false, false, forQuery);
                        }

                        if (!String.IsNullOrWhiteSpace(queryString))
                        {
                            content = content.Replace("<img src=\"/preview_image.aspx", $"<img data=\"{queryString}\" src=\"/preview_image.aspx");
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, content);
                    }
                    else
                    {
                        var template = await GetTemplateAsync(name: templateName);
                        var values = queryString.Split('&', StringSplitOptions.RemoveEmptyEntries).Select(x => new KeyValuePair<string, string>(x.Split('=')[0], x.Split('=')[1]));
                        var content = stringReplacementsService.DoReplacements(template.Content, values, forQuery);
                        if (handleStringReplacements)
                        {
                            content = await stringReplacementsService.DoAllReplacementsAsync(content, dataRow, handleRequest, false, false, forQuery);
                        }

                        if (!String.IsNullOrWhiteSpace(queryString))
                        {
                            content = content.Replace("<img src=\"/preview_image.aspx", $"<img data=\"{queryString}\" src=\"/preview_image.aspx");
                        }

                        input = input.ReplaceCaseInsensitive(m.Groups[0].Value, content);
                    }
                }
            }

            return input;
        }

        /// <inheritdoc />
        public async Task<DynamicContent> GetDynamicContentData(int contentId)
        {
            string query = null;
            var templateVersionPart = "";
            switch (gclSettings.Environment)
            {
                case Environments.Development:
                    // Always get the latest version on development.
                    query = @"SELECT 
                                d.filledvariables, 
                                d.freefield1,
                                d.type,
                                d.version
                            FROM easy_dynamiccontent d
                            JOIN (SELECT id, MAX(version) AS version FROM easy_dynamiccontent GROUP BY id) d2 ON d2.id = d.id AND d2.version = d.version
                            WHERE d.id = ?contentId";
                    break;
                case Environments.Test:
                    templateVersionPart = "AND t.istest = 1";
                    break;
                case Environments.Acceptance:
                    templateVersionPart = "AND t.isacceptance = 1";
                    break;
                case Environments.Live:
                    templateVersionPart = "AND t.islive = 1";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            query ??= $@"SELECT 
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        JOIN easy_templates t ON t.itemid = d.itemid AND t.version = d.version {templateVersionPart}
                        WHERE d.id = ?contentId

                        UNION

                        SELECT 
                            d.filledvariables, 
                            d.freefield1,
                            d.type,
                            d.version
                        FROM easy_dynamiccontent d
                        WHERE d.version = 1 
                        AND d.itemid = 0
                        AND d.id = ?contentId";
            
            databaseConnection.AddParameter("contentId", contentId);
            var dataTable = await databaseConnection.GetAsync(query);
            if (dataTable.Rows.Count == 0)
            {
                return null;
            }

            return new DynamicContent
            {
                Id = contentId,
                Name = dataTable.Rows[0].Field<string>("freefield1"),
                SettingsJson = dataTable.Rows[0].Field<string>("filledvariables"),
                Type = dataTable.Rows[0].Field<int>("type"),
                Version = dataTable.Rows[0].Field<int>("version")
            };
        }

        /// <inheritdoc />
        public async Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            if (String.IsNullOrWhiteSpace(dynamicContent?.Name) || String.IsNullOrWhiteSpace(dynamicContent?.SettingsJson))
            {
                return ("", null);
            }

            string viewComponentName;
            switch (dynamicContent.Name)
            {
                case "GeeksCoreLibrary.Repeater":
                case "JuiceControlLibrary.MLSimpleMenu":
                case "JuiceControlLibrary.SimpleMenu":
                case "JuiceControlLibrary.ProductModule":
                    {
                        viewComponentName = "Repeater";
                        break;
                    }
                case "GeeksCoreLibrary.Account":
                case "JuiceControlLibrary.AccountWiser2":
                    {
                        viewComponentName = "Account";
                        break;
                    }
                case "GeeksCoreLibrary.ShoppingBasket":
                case "JuiceControlLibrary.ShoppingBasket":
                    {
                        viewComponentName = "ShoppingBasket";
                        break;
                    }
                case "GeeksCoreLibrary.WebPage":
                case "JuiceControlLibrary.WebPage":
                    {
                        viewComponentName = "WebPage";
                        break;
                    }
                case "GeeksCoreLibrary.Pagination":
                case "JuiceControlLibrary.Pagination":
                    {
                        viewComponentName = "Pagination";
                        break;
                    }
                case "GeeksCoreLibrary.Filter":
                case "JuiceControlLibrary.DynamicFilter":
                    {
                        viewComponentName = "Filter";
                        break;
                    }
                case "GeeksCoreLibrary.WebForm":
                case "JuiceControlLibrary.Sendform":
                    {
                        viewComponentName = "WebForm";
                        break;
                    }
                case "GeeksCoreLibrary.Configurator":
                case "JuiceControlLibrary.Configurator":
                {
                    viewComponentName = "Configurator";
                    break;
                }
                default:
                    return ($"<!-- Dynamic content type '{dynamicContent.Name}' not supported yet. Content ID: {dynamicContent.Id} -->", null);
            }

            // Create a fake ViewContext (but with a real ActionContext and a real HttpContext).
            var viewContext = new ViewContext(
                actionContextAccessor.ActionContext,
                NullView.Instance,
                new ViewDataDictionary(new EmptyModelMetadataProvider(), new ModelStateDictionary()),
                new TempDataDictionary(httpContextAccessor.HttpContext, tempDataProvider),
                TextWriter.Null,
                new HtmlHelperOptions());

            // Set the context in the ViewComponentHelper, so that the ViewComponents that we use actually have the proper context.
            (viewComponentHelper as IViewContextAware)?.Contextualize(viewContext);

            // Dynamically invoke the correct ViewComponent.
            var component = await viewComponentHelper.InvokeAsync(viewComponentName, new { dynamicContent, callMethod, forcedComponentMode, extraData });

            // If there is a InvokeMethodResult, it means this that a specific method on a specific component was called via /gclcomponent.gcl
            // and we only want to return the results of that method, instead of rendering the entire component.
            if (viewContext.TempData.ContainsKey("InvokeMethodResult") && viewContext.TempData["InvokeMethodResult"] != null)
            {
                return (viewContext.TempData["InvokeMethodResult"], viewContext.ViewData);
            }

            await using var stringWriter = new StringWriter();
            component.WriteTo(stringWriter, HtmlEncoder.Default);
            var html = stringWriter.ToString();

            return (html, viewContext.ViewData);
        }

        /// <inheritdoc />
        public async Task<(object result, ViewDataDictionary viewData)> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null)
        {
            var dynamicContent = await GetDynamicContentData(componentId);
            return await GenerateDynamicContentHtmlAsync(dynamicContent, forcedComponentMode, callMethod, extraData);
        }

        /// <inheritdoc />
        public async Task<string> ReplaceAllDynamicContentAsync(string template)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return template;
            }

            // TODO: Test the speed of this and see if it's better run a while loop on string.Contains("contentid=") instead of the regular expression.
            // Timeout on the regular expression to prevent denial of service attacks.
            var regEx = new Regex(@"<img[^>]*?(?:data=['""](?<data>.*?)['""][^>]*?)?contentid=['""](?<contentid>\d+)['""][^>]*?/?>", RegexOptions.Compiled | RegexOptions.Singleline | RegexOptions.IgnoreCase, TimeSpan.FromMinutes(3));

            var matches = regEx.Matches(template);
            foreach (Match match in matches)
            {
                if (!match.Success)
                {
                    continue;
                }

                if (!Int32.TryParse(match.Groups["contentid"].Value, out var contentId) || contentId <= 0)
                {
                    logger.LogWarning($"Found dynamic content with invalid contentId of '{match.Groups["contentid"].Value}', so ignoring it.");
                    continue;
                }

                try
                {
                    var extraData = match.Groups["data"].Value?.ToDictionary("&", "=");
                    var (html, _) = await GenerateDynamicContentHtmlAsync(contentId, extraData: extraData);
                    template = template.Replace(match.Value, (string)html);
                }
                catch (Exception exception)
                {
                    logger.LogError($"An error while generating component with id '{contentId}': {exception}");
                    var errorOnPage = $"An error occurred while generating component with id '{contentId}'";
                    if (gclSettings.Environment == Environments.Development || gclSettings.Environment == Environments.Test)
                    {
                        errorOnPage += $": {exception.Message}";
                    }

                    template = template.Replace(match.Value, errorOnPage);
                }
            }

            return template;
        }

        /// <inheritdoc />
        public async Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false)
        {
            var query = queryTemplate?.Content;
            if (String.IsNullOrWhiteSpace(query))
            {
                return null;
            }
            
            queryTemplate.GroupingSettings ??= new QueryGroupingSettings();
            query = await DoReplacesAsync(query, true, false, true, null, true, false, true);
            if (query.Contains("{filters}", StringComparison.OrdinalIgnoreCase))
            {
                query = query.ReplaceCaseInsensitive("{filters}", (await filtersService.GetFilterQueryPartAsync()).JoinPart);
            }

            var pusherRegex = new Regex(@"PUSHER<channel\((.*?)\),event\((.*?)\),message\(((?s:.)*?)\)>", RegexOptions.Compiled);
            var pusherMatches = pusherRegex.Matches(query);
            foreach (Match match in pusherMatches)
            {
                query = query.Replace(match.Value, "");
            }

            if (recursive)
            {
                queryTemplate.GroupingSettings.GroupingColumn = "id";
            }

            var dataTable = await databaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count == 0 ? new JArray() : dataTable.ToJsonArray(queryTemplate.GroupingSettings, encryptionKey, skipNullValues, allowValueDecryption, recursive);

            if (pusherMatches.Any())
            {
                throw new NotImplementedException("Pusher messages not yet implemented");
            }

            return result;
        }

        /// <summary>
        /// Do all replacement which have to do with request, session or cookie.
        /// Only use this function if you can't add ITemplatesService via dependency injection, otherwise you should use the non static functions <see cref="IStringReplacementsService.DoSessionReplacements" /> and <see cref="IStringReplacementsService.DoHttpRequestReplacements"/>.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="httpContext"></param>
        public static string DoHttpContextReplacements(string input, HttpContext httpContext)
        {
            // Querystring replaces.
            foreach (var key in httpContext.Request.Query.Keys)
            {
                input = input.ReplaceCaseInsensitive($"{{{key}}}", httpContext.Request.Query[key]);
            }

            // Form replaces.
            if (httpContext.Request.HasFormContentType)
            {
                foreach (var variable in httpContext.Request.Form.Keys)
                {
                    input = input.ReplaceCaseInsensitive($"{{{variable}}}", httpContext.Request.Form[variable]);
                }
            }

            // Session replaces.
            foreach (var variable in httpContext.Session.Keys)
            {
                input = input.ReplaceCaseInsensitive($"{{{variable}}}", httpContext.Session.GetString(variable));
            }

            // Cookie replaces.
            foreach (var key in httpContext.Request.Cookies.Keys)
            {
                input = input.ReplaceCaseInsensitive($"{{{key}}}", httpContext.Request.Cookies[key]);
            }

            return input;
        }
    }
}
