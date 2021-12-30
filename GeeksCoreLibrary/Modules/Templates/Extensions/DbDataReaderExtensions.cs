using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Modules.Templates.Extensions
{
    public static class DbDataReaderExtensions
    {
        public static async Task<Template> ToTemplateModelAsync(this DbDataReader reader, TemplateTypes type = TemplateTypes.Html)
        {
            if (!Enum.TryParse(typeof(TemplateTypes), reader.GetStringHandleNull("templateType"), true, out var templateType))
            {
                if (!Enum.TryParse(typeof(TemplateTypes), reader.GetStringHandleNull("rootName"), true, out templateType))
                {
                    templateType = TemplateTypes.Unknown;
                }
            }

            if (!Enum.TryParse(typeof(ResourceInsertModes), reader.GetStringHandleNull("pagemode"), true, out var insertMode))
            {
                insertMode = ResourceInsertModes.Standard;
            }

            if (!Enum.TryParse(typeof(TemplateCachingModes), reader.GetStringHandleNull("usecache"), true, out var cachingMode))
            {
                cachingMode = TemplateCachingModes.NoCaching;
            }

            var isQuery = type == TemplateTypes.Query || templateType is TemplateTypes.Query;
            var template = isQuery ? new QueryTemplate() : new Template();
            template.Id = await reader.GetFieldValueAsync<int>("templateId");
            template.ParentId = await reader.GetFieldValueAsync<int>("parentId");
            template.RootName = reader.GetStringHandleNull("rootName");
            template.ParentName = reader.GetStringHandleNull("parentName");
            template.Name = reader.GetStringHandleNull("name");
            template.Type = (TemplateTypes)(templateType ?? TemplateTypes.Unknown);
            template.InsertMode = (ResourceInsertModes)(insertMode ?? ResourceInsertModes.Standard);
            template.SortOrder = await reader.GetFieldValueAsync<int>("volgnr");
            template.ParentSortOrder = await reader.GetFieldValueAsync<int>("parentOrder");
            template.LoadAlways = await reader.GetFieldValueAsync<int>("loadalways") > 0;
            template.LastChanged = await reader.GetFieldValueAsync<DateTime>("lastchanged");
            template.UrlRegex = reader.GetStringHandleNull("urlregex");
            template.CachingMode = (TemplateCachingModes)(cachingMode ?? TemplateCachingModes.NoCaching);
            template.CachingMinutes = await reader.GetFieldValueAsync<int>("cacheminutes");

            if (template.Type == TemplateTypes.Scss)
            {
                template.Type = TemplateTypes.Css;
            }

            var useObfuscate = Convert.ToInt16(await reader.GetFieldValueAsync<object>("useobfuscate")) > 0;
            var htmlMinified = reader.GetStringHandleNull("html_minified");
            var html = reader.GetStringHandleNull("html");
            if (useObfuscate)
            {
                template.Content = reader.GetStringHandleNull("html_obfuscated");
            }
            else if (!String.IsNullOrWhiteSpace(htmlMinified))
            {
                template.Content = htmlMinified;
            }
            else if (!String.IsNullOrWhiteSpace(html))
            {
                template.Content = html;
            }
            else
            {
                template.Content = reader.GetStringHandleNull("template");
            }

            var cssTemplates = reader.GetStringHandleNull("csstemplates");
            if (!String.IsNullOrWhiteSpace(cssTemplates))
            {
                template.CssTemplates = cssTemplates.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var javascriptTemplates = reader.GetStringHandleNull("jstemplates");
            if (!String.IsNullOrWhiteSpace(javascriptTemplates))
            {
                template.JavascriptTemplates = javascriptTemplates.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var externalFiles = reader.GetStringHandleNull("externalfiles");
            if (!String.IsNullOrWhiteSpace(externalFiles))
            {
                template.ExternalFiles = externalFiles.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var cdnFiles = reader.GetStringHandleNull("defaulttemplate");
            if (!String.IsNullOrWhiteSpace(cdnFiles))
            {
                template.WiserCdnFiles = cdnFiles.Split(';', StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (!isQuery)
            {
                return template;
            }

            var queryTemplate = (QueryTemplate)template;
            queryTemplate.GroupingSettings = new QueryGroupingSettings
            {
                GroupingFieldsPrefix = reader.GetStringHandleNull("groupingprefix"),
                ObjectInsteadOfArray = reader.GetBoolean("groupingCreateObjectInsteadOfArray"),
                GroupingColumn = reader.GetStringHandleNull("groupingkey"),
                GroupingValueColumnName = reader.GetStringHandleNull("groupingValueColumnName"),
                GroupingKeyColumnName = reader.GetStringHandleNull("groupingKeyColumnName")
            };

            return queryTemplate;
        }
    }
}
