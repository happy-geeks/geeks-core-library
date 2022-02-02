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
            if (!Enum.TryParse(typeof(TemplateTypes), reader.GetStringHandleNull("template_type"), true, out var templateType))
            {
                if (!Enum.TryParse(typeof(TemplateTypes), reader.GetStringHandleNull("root_name"), true, out templateType))
                {
                    templateType = TemplateTypes.Unknown;
                }
            }

            if (!Enum.TryParse(typeof(ResourceInsertModes), reader.GetStringHandleNull("insert_mode"), true, out var insertMode))
            {
                insertMode = ResourceInsertModes.Standard;
            }

            if (!Enum.TryParse(typeof(TemplateCachingModes), reader.GetStringHandleNull("use_cache"), true, out var cachingMode))
            {
                cachingMode = TemplateCachingModes.NoCaching;
            }

            var isQuery = type == TemplateTypes.Query || templateType is TemplateTypes.Query;
            var template = isQuery ? new QueryTemplate() : new Template();
            template.Id = await reader.IsDBNullAsync("template_id") ? 0 : await reader.GetFieldValueAsync<int>("template_id");
            template.ParentId = await reader.IsDBNullAsync("parent_id") ? 0 : await reader.GetFieldValueAsync<int>("parent_id");
            template.RootName = reader.GetStringHandleNull("root_name");
            template.ParentName = reader.GetStringHandleNull("parent_name");
            template.Name = reader.GetStringHandleNull("template_name");
            template.Type = (TemplateTypes)(templateType ?? TemplateTypes.Unknown);
            template.InsertMode = (ResourceInsertModes)(insertMode ?? ResourceInsertModes.Standard);
            template.SortOrder = await reader.IsDBNullAsync("ordering") ? 0 : await reader.GetFieldValueAsync<int>("ordering");
            template.ParentSortOrder = await reader.IsDBNullAsync("parent_ordering") ? 0 : await reader.GetFieldValueAsync<int>("parent_ordering");
            template.LoadAlways = Convert.ToBoolean(reader.GetValue("load_always"));
            template.LastChanged = await reader.GetFieldValueAsync<DateTime>("changed_on");
            template.UrlRegex = reader.GetStringHandleNull("url_regex");
            template.CachingMode = (TemplateCachingModes)(cachingMode ?? TemplateCachingModes.NoCaching);
            template.CachingMinutes = await reader.IsDBNullAsync("cache_minutes") ? 0 : await reader.GetFieldValueAsync<int>("cache_minutes");

            if (template.Type == TemplateTypes.Scss)
            {
                template.Type = TemplateTypes.Css;
            }

            var useObfuscate = Convert.ToInt16(await reader.GetFieldValueAsync<object>("use_obfuscate")) > 0;
            var htmlMinified = reader.GetStringHandleNull("template_data_minified");
            var html = reader.GetStringHandleNull("template_data");
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
            else if (reader.HasColumn("template"))
            {
                template.Content = reader.GetStringHandleNull("template");
            }

            var cssTemplates = reader.GetStringHandleNull("css_templates");
            if (!String.IsNullOrWhiteSpace(cssTemplates))
            {
                template.CssTemplates = cssTemplates.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var javascriptTemplates = reader.GetStringHandleNull("javascript_templates");
            if (!String.IsNullOrWhiteSpace(javascriptTemplates))
            {
                template.JavascriptTemplates = javascriptTemplates.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var externalFiles = reader.GetStringHandleNull("external_files");
            if (!String.IsNullOrWhiteSpace(externalFiles))
            {
                template.ExternalFiles = externalFiles.Split(";", StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var cdnFiles = reader.HasColumn("wiser_cdn_files") ? reader.GetStringHandleNull("wiser_cdn_files") : null;
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
                GroupingFieldsPrefix = reader.GetStringHandleNull("grouping_prefix"),
                ObjectInsteadOfArray = reader.GetBoolean("grouping_create_object_instead_of_array"),
                GroupingColumn = reader.GetStringHandleNull("grouping_key"),
                GroupingValueColumnName = reader.GetStringHandleNull("grouping_value_column_name"),
                GroupingKeyColumnName = reader.GetStringHandleNull("grouping_key_column_name")
            };

            return queryTemplate;
        }
    }
}
