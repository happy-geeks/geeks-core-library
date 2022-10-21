using System;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;

namespace GeeksCoreLibrary.Modules.Templates.Extensions
{
    public static class DbDataReaderExtensions
    {
        public static async Task<Template> ToTemplateModelAsync(this DbDataReader reader, TemplateTypes? type = null)
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

            var isQuery = type is TemplateTypes.Query || templateType is TemplateTypes.Query;
            var isRoutine = type is TemplateTypes.Routine || templateType is TemplateTypes.Routine;
            var template = isQuery ? new QueryTemplate() : isRoutine ? new RoutineTemplate() : new Template();
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
            template.UrlRegex = reader.GetStringHandleNull("url_regex");
            template.CachingMode = (TemplateCachingModes)(cachingMode ?? TemplateCachingModes.NoCaching);
            template.CachingMinutes = await reader.IsDBNullAsync("cache_minutes") ? 0 : await reader.GetFieldValueAsync<int>("cache_minutes");
            template.CachingLocation = !reader.HasColumn("caching_location") || await reader.IsDBNullAsync("caching_location") ? TemplateCachingLocations.InMemory : (TemplateCachingLocations)await reader.GetFieldValueAsync<int>("caching_location");
            template.CachingRegex = reader.GetStringHandleNull("cache_regex");

            if (!await reader.IsDBNullAsync("changed_on"))
            {
                template.LastChanged = await reader.GetFieldValueAsync<DateTime>("changed_on");
            }

            if (template.Type == TemplateTypes.Scss)
            {
                template.Type = TemplateTypes.Css;
            }

            var useObfuscate = Convert.ToInt16(await reader.GetFieldValueAsync<object>("use_obfuscate")) > 0;
            var htmlMinified = reader.HasColumn("template_data_minified") ? reader.GetStringHandleNull("template_data_minified") : "";
            var html = reader.HasColumn("template_data") ? reader.GetStringHandleNull("template_data") : "";
            if (useObfuscate && reader.HasColumn("html_obfuscated"))
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
                template.CssTemplates = cssTemplates.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var javascriptTemplates = reader.GetStringHandleNull("javascript_templates");
            if (!String.IsNullOrWhiteSpace(javascriptTemplates))
            {
                template.JavascriptTemplates = javascriptTemplates.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).Select(Int32.Parse).Where(id => id > 0).ToList();
            }

            var externalFiles = reader.GetStringHandleNull("external_files");
            if (!String.IsNullOrWhiteSpace(externalFiles))
            {
                template.ExternalFiles = externalFiles.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            var cdnFiles = reader.HasColumn("wiser_cdn_files") ? reader.GetStringHandleNull("wiser_cdn_files") : null;
            if (!String.IsNullOrWhiteSpace(cdnFiles))
            {
                template.WiserCdnFiles = cdnFiles.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries).ToList();
            }

            if (reader.HasColumn("pre_load_query"))
            {
                template.PreLoadQuery = reader.GetStringHandleNull("pre_load_query");
            }

            if (reader.HasColumn("return_not_found_when_pre_load_query_has_no_data"))
            {
                template.ReturnNotFoundWhenPreLoadQueryHasNoData = Convert.ToInt16(await reader.GetFieldValueAsync<object>("return_not_found_when_pre_load_query_has_no_data")) > 0;
            }

            if (reader.HasColumn("login_required"))
            {
                template.LoginRequired = Convert.ToBoolean(await reader.GetFieldValueAsync<object>("login_required"));
            }

            if (reader.HasColumn("login_role"))
            {
                if (await reader.IsDBNullAsync(reader.GetOrdinal("login_role")))
                {
                    template.LoginRoles = null;
                }
                else
                {
                    template.LoginRoles = (await reader.GetFieldValueAsync<string>("login_role"))?.Split(",", StringSplitOptions.RemoveEmptyEntries).Select(i => Convert.ToInt32(i)).ToList();
                }
            }

            if (reader.HasColumn("login_redirect_url"))
            {
                template.LoginRedirectUrl = reader.GetStringHandleNull("login_redirect_url");
            }

            if (reader.HasColumn("is_default_header"))
            {
                template.IsDefaultHeader = Convert.ToBoolean(await reader.GetFieldValueAsync<object>("is_default_header"));
            }

            if (reader.HasColumn("is_default_footer"))
            {
                template.IsDefaultFooter = Convert.ToBoolean(await reader.GetFieldValueAsync<object>("is_default_footer"));
            }

            if (reader.HasColumn("default_header_footer_regex"))
            {
                template.DefaultHeaderFooterRegex = reader.GetStringHandleNull("default_header_footer_regex");
            }

            if (isQuery)
            {
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

            if (isRoutine)
            {
                var routineTemplate = (RoutineTemplate)template;
                routineTemplate.RoutineType = (RoutineTypes) await reader.GetFieldValueAsync<int>("routine_type");
                routineTemplate.RoutineParameters = reader.GetStringHandleNull("routine_parameters");
                routineTemplate.RoutineReturnType = reader.GetStringHandleNull("routine_return_type");
                
                return routineTemplate;
            }


            return template;
        }
    }
}
