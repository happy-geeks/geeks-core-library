﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.Templates.Enums;
using GeeksCoreLibrary.Modules.Templates.Models;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Templates.Interfaces;

public interface ITemplatesService
{
    /// <summary>
    /// Search for a template and returns it. Must supply either an ID or a name.<br/>
    /// This will return the unrendered template, with the content as it is in the database. Use <see cref="IPagesService.GetRenderedTemplateAsync"/> for the rendered version.
    /// </summary>
    /// <param name="id">Optional: The ID of the template to get.</param>
    /// <param name="name">Optional: The name of the template to get.</param>
    /// <param name="type">Optional: The type of template that is being searched for. Only used in combination with name. Default value is null, which is all template types.</param>
    /// <param name="parentId">Optional: The ID of the parent of the template to get.</param>
    /// <param name="parentName">Optional: The name of the parent of template to get.</param>
    /// <param name="includeContent">Optional: Whether to include the contents of the template. Default value is <see langword="true"/>.</param>
    /// <param name="skipPermissions">Optional: Whether to skip the check if the user has the permissions to see the template</param>
    /// <returns>The <see cref="Template"/> object with all it's data.</returns>
    Task<Template> GetTemplateAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "", bool includeContent = true, bool skipPermissions = false);

    /// <summary>
    /// Get only the contents and ID of a template. Must supply either an ID or a name.
    /// </summary>
    /// <param name="id">Optional: The ID of the template to get.</param>
    /// <param name="name">Optional: The name of the template to get.</param>
    /// <param name="type">Optional: The type of template that is being searched for. Only used in combination with name. Default value is null, which is all template types.</param>
    /// <param name="parentId">Optional: The ID of the parent of the template to get.</param>
    /// <param name="parentName">Optional: The name of the parent of template to get.</param>
    /// <returns>The <see cref="Template"/> object with only the contents filled.</returns>
    Task<Template> GetTemplateContentAsync(int id = 0, string name = "", TemplateTypes? type = null, int parentId = 0, string parentName = "");

    /// <summary>
    /// Gets the caching settings for a template.
    /// </summary>
    /// <param name="id">Optional: The ID of the template to get.</param>
    /// <param name="name">Optional: The name of the template to get.</param>
    /// <param name="parentId">Optional: The ID of the parent of the template to get.</param>
    /// <param name="parentName">Optional: The name of the parent of template to get.</param>
    /// <returns>The <see cref="Template"/> object with only the caching settings filled.</returns>
    Task<Template> GetTemplateCacheSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "");

    /// <summary>
    /// Gets the ID of a template based on the name and type of template.
    /// </summary>
    /// <param name="name">The name of the template.</param>
    /// <param name="type">The type of template.</param>
    /// <returns>The ID of the template or <see langword="0"/> if it wasn't found.</returns>
    Task<int> GetTemplateIdFromNameAsync(string name, TemplateTypes type);

    /// <summary>
    /// Gets the last changed date of general templates of a specific type. This can be used for generating the URL for gcl_general.css for example.
    /// </summary>
    /// <param name="templateType">Optional: The template type to get the last change date of. Default is Css.</param>
    /// <param name="byInsertMode">Optional: Which insert mode the templates should have. Defaults to <see cref="ResourceInsertModes.Standard"/>.</param>
    /// <returns>Null if there are no general templates of the specified type, or the date of the most recent change in all the general templates of the specified type.</returns>
    Task<DateTime?> GetGeneralTemplateLastChangedDateAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard);

    /// <summary>
    /// Get the content for the general CSS or javascript file that needs to be loaded on every page.
    /// </summary>
    /// <param name="templateType">The type of content to get.</param>
    /// <param name="byInsertMode">Optional: Which insert mode the templates should have. Defaults to <see cref="ResourceInsertModes.Standard"/>.</param>
    /// <returns>A <see cref="TemplateResponse"/> object that contains the combined contents of all global CSS or javascript templates.</returns>
    Task<TemplateResponse> GetGeneralTemplateValueAsync(TemplateTypes templateType, ResourceInsertModes byInsertMode = ResourceInsertModes.Standard);

    /// <summary>
    /// Get one or more templates.
    /// </summary>
    /// <param name="templateIds">The IDs of templates to get.</param>
    /// <param name="includeContent">Set to true to also get the complete content of the template, false to only get the meta-data.</param>
    /// <returns>A list of <see cref="Template"/>s.</returns>
    Task<List<Template>> GetTemplatesAsync(ICollection<int> templateIds, bool includeContent);

    /// <summary>
    /// Get the content for multiple templates and combine them into one string.
    /// </summary>
    /// <param name="templateIds">The IDs of the templates to get.</param>
    /// <param name="templateType">The type of content to get.</param>
    /// <returns>A <see cref="TemplateResponse"/> object that contains the combined contents of all specified templates.</returns>
    Task<TemplateResponse> GetCombinedTemplateValueAsync(ICollection<int> templateIds, TemplateTypes templateType);

    /// <summary>
    /// Get the content for multiple templates and combine them into one string.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GenerateDynamicContentHtmlAsync() in this method.</param>
    /// <param name="templateIds">The IDs of the templates to get.</param>
    /// <param name="templateType">The type of content to get.</param>
    /// <returns>A <see cref="TemplateResponse"/> object that contains the combined contents of all specified templates.</returns>
    Task<TemplateResponse> GetCombinedTemplateValueAsync(ITemplatesService templatesService, ICollection<int> templateIds, TemplateTypes templateType);

    /// <summary>
    /// Adds a single template to the response for a combined template response.
    /// </summary>
    /// <param name="idsLoaded">A list of IDs, to keep track of </param>
    /// <param name="template">The template to add/</param>
    /// <param name="currentUrl">The URL of the current request.</param>
    /// <param name="resultBuilder">A <see cref="StringBuilder"/> to use for combining all templates.</param>
    /// <param name="templateResponse">A <see cref="TemplateResponse"/> object that will contain the combined contents of all specified templates.</param>
    Task AddTemplateToResponseAsync(ICollection<int> idsLoaded, Template template, string currentUrl, StringBuilder resultBuilder, TemplateResponse templateResponse);

    /// <summary>
    /// Gets the content for resources that are loaded from the Wiser CDN.
    /// </summary>
    /// <param name="fileNames">The list of file names to load.</param>
    /// <returns>The combined contents of the specified files from the old legacy Wiser CDN.</returns>
    Task<string> GetWiserCdnFilesAsync(ICollection<string> fileNames);

    /// <summary>
    /// Do all replaces on a template. If you don't need to replace includes, please use IStringReplacementsService.DoAllReplacements() instead.
    /// This function will call IStringReplacementsService.DoAllReplacements() and then handle includes after.
    /// For example:
    /// Replaces template names (syntax as: &lt;[templateName]&gt;) with template from cache (easy-templates)
    /// For backward compatibility reasons also: replaces templates (syntax as: &lt;[parentTemplateName\templateName]&gt;) with template from cache (easy-templates)
    /// </summary>
    /// <param name="input">The original content to be replaced</param>
    /// <param name="handleStringReplacements">Optional: Whether string replacements should be performed on the template.</param>
    /// <param name="handleDynamicContent">Optional: Whether to replace dynamic content blocks in the template. Disabling this improves performance. Default value is true.</param>
    /// <param name="evaluateLogicSnippets">Optional: Whether to evaluate any logic snippets in the template. Disabling this improves performance. Default value is true.</param>
    /// <param name="dataRow">Optional: All values from this <see cref="DataRow"/> will also be replaced in the output.</param>
    /// <param name="handleRequest">Optional: Whether to replace values from the request (such as query string, cookies and session). Default value is true.</param>
    /// <param name="removeUnknownVariables">Optional: Whether ot not to remove all leftover variables after all replacements have been done. Default value is true.</param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="templateType">Optional: Limit which template type can be used for template includes. Use null for all template types.</param>
    /// <param name="handleVariableDefaults">Optional: Handle variable defaults (such as {name~Bob}), which will place the value "Bob" on that position, if the name variable is empty or doesn't exist. Default is true.</param>
    /// <returns>The original string with all replacements done.</returns>
    Task<string> DoReplacesAsync(string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true);

    /// <summary>
    /// Do all replaces on a template. If you don't need to replace includes, please use IStringReplacementsService.DoAllReplacements() instead.
    /// This function will call IStringReplacementsService.DoAllReplacements() and then handle includes after.
    /// For example:
    /// Replaces template names (syntax as: &lt;[templateName]&gt;) with template from cache (easy-templates)
    /// For backward compatibility reasons also: replaces templates (syntax as: &lt;[parentTemplateName\templateName]&gt;) with template from cache (easy-templates)
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GenerateDynamicContentHtmlAsync() in this method.</param>
    /// <param name="input">The original content to be replaced</param>
    /// <param name="handleStringReplacements">Optional: Whether string replacements should be performed on the template.</param>
    /// <param name="handleDynamicContent">Optional: Whether to replace dynamic content blocks in the template. Disabling this improves performance. Default value is true.</param>
    /// <param name="evaluateLogicSnippets">Optional: Whether to evaluate any logic snippets in the template. Disabling this improves performance. Default value is true.</param>
    /// <param name="dataRow">Optional: All values from this <see cref="DataRow"/> will also be replaced in the output.</param>
    /// <param name="handleRequest">Optional: Whether to replace values from the request (such as query string, cookies and session). Default value is true.</param>
    /// <param name="removeUnknownVariables">Optional: Whether ot not to remove all leftover variables after all replacements have been done. Default value is true.</param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="templateType">Optional: Limit which template type can be used for template includes. Use null for all template types.</param>
    /// <param name="handleVariableDefaults">Optional: Handle variable defaults (such as {name~Bob}), which will place the value "Bob" on that position, if the name variable is empty or doesn't exist. Default is true.</param>
    /// <returns>The original string with all replacements done.</returns>
    Task<string> DoReplacesAsync(ITemplatesService templatesService, string input, bool handleStringReplacements = true, bool handleDynamicContent = true, bool evaluateLogicSnippets = true, DataRow dataRow = null, bool handleRequest = true, bool removeUnknownVariables = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true);

    /// <summary>
    /// Replaces [include[x]] with a template called 'x'.
    /// </summary>
    /// <param name="input">The string that might have an include.</param>
    /// <param name="handleStringReplacements">Optional: Whether string replacements should be performed on the included template(s).</param>
    /// <param name="dataRow">Optional: All values from this <see cref="DataRow"/> will also be replaced in the output in the included template(s).</param>
    /// <param name="handleRequest">Optional: Whether to replace values from the request (such as query string, cookies and session) in the included template(s). Default value is true.</param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="templateType">Optional: Limit which template type can be included. Use null for all template types.</param>
    /// <param name="handleVariableDefaults">Optional: Handle variable defaults (such as {name~Bob}), which will place the value "Bob" on that position, if the name variable is empty or doesn't exist. Default is true.</param>
    /// <returns>The original string with all replacements done.</returns>
    Task<string> HandleIncludesAsync(string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true);

    /// <summary>
    /// Replaces [include[x]] with a template called 'x'.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GenerateDynamicContentHtmlAsync() in this method.</param>
    /// <param name="input">The string that might have an include.</param>
    /// <param name="handleStringReplacements">Optional: Whether string replacements should be performed on the included template(s).</param>
    /// <param name="dataRow">Optional: All values from this <see cref="DataRow"/> will also be replaced in the output in the included template(s).</param>
    /// <param name="handleRequest">Optional: Whether to replace values from the request (such as query string, cookies and session) in the included template(s). Default value is true.</param>
    /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
    /// <param name="templateType">Optional: Limit which template type can be included. Use null for all template types.</param>
    /// <param name="handleVariableDefaults">Optional: Handle variable defaults (such as {name~Bob}), which will place the value "Bob" on that position, if the name variable is empty or doesn't exist. Default is true.</param>
    /// <returns>The original string with all replacements done.</returns>
    Task<string> HandleIncludesAsync(ITemplatesService templatesService, string input, bool handleStringReplacements = true, DataRow dataRow = null, bool handleRequest = true, bool forQuery = false, TemplateTypes? templateType = null, bool handleVariableDefaults = true);

    /// <summary>
    /// Generates the HTML for dynamic content, based on the content ID.
    /// </summary>
    /// <param name="componentId">The ID of the dynamic content.</param>
    /// <param name="forcedComponentMode">Optional: If you want to overwrite the component mode of the component. Default is <see langword="null" />.</param>
    /// <param name="callMethod">Optional: If you want to call a specific method in the component, enter the name of that method here.</param>
    /// <param name="extraData">Optional: Any extra data to be used in all replacements in the component.</param>
    /// <returns>The generated HTML of the component or the result of the called method of the component.</returns>
    Task<object> GenerateDynamicContentHtmlAsync(int componentId, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null);

    /// <summary>
    /// Generates the HTML for dynamic content, based on the content ID.
    /// </summary>
    /// <param name="dynamicContent">The data of the dynamic content.</param>
    /// <param name="forcedComponentMode">Optional: If you want to overwrite the component mode of the component. Default is <see langword="null" />.</param>
    /// <param name="callMethod">Optional: If you want to call a specific method in the component, enter the name of that method here.</param>
    /// <param name="extraData">Optional: Any extra data to be used in all replacements in the component.</param>
    /// <returns>The generated HTML of the component or the result of the called method of the component.</returns>
    Task<object> GenerateDynamicContentHtmlAsync(DynamicContent dynamicContent, int? forcedComponentMode = null, string callMethod = null, Dictionary<string, string> extraData = null);

    /// <summary>
    /// Replaces all image templates with the actual image HTML, which contains multiple image URLs for different formats.
    /// </summary>
    /// <param name="input">The string to render the images in.</param>
    /// <returns>The original string with the image template replacements done.</returns>
    Task<string> HandleImageTemplating(string input);

    /// <summary>
    /// Generates an image URL for a specific item, type, number and filename.
    /// </summary>
    /// <param name="itemId">The ID of the Wiser item that the image is linked to.</param>
    /// <param name="type">The image type.</param>
    /// <param name="number">The ordering number of the image to use, in case there are multiple images uploaded. Use <c>0</c> if this doesn't matter.</param>
    /// <param name="filename">The name of the file, including extension.</param>
    /// <param name="width">The width that the image should be, in pixels.</param>
    /// <param name="height">The height that the image should be, in pixels.</param>
    /// <param name="resizeMode">The resize mode to use (e.g. crop).</param>
    /// <param name="fileType">The type of file.</param>
    /// <returns>The URL to the specified image in the specified size, that can be added to the HTML on a web page.</returns>
    Task<string> GenerateImageUrl(string itemId, string type, int number, string filename = "", string width = "0", string height = "0", string resizeMode = "", string fileType = "");

    /// <summary>
    /// Gets the data for dynamic content, from "easy_dynamiccontent".
    /// </summary>
    /// <param name="contentId">The ID of the dynamic content.</param>
    /// <returns>A Tuple with the content type and settings JSON.</returns>
    Task<DynamicContent> GetDynamicContentData(int contentId);

    /// <summary>
    /// Replaces all dynamic content in the given template and returns the new template.
    /// </summary>
    /// <param name="template">The template to replace the dynamic content in.</param>
    /// <param name="componentOverrides">Optional: If you already have the settings for one or more components, you can add them here. This is made to he </param>
    /// <returns>The new template with all dynamic content.</returns>
    Task<string> ReplaceAllDynamicContentAsync(string template, List<DynamicContent> componentOverrides = null);

    /// <summary>
    /// Replaces all dynamic content in the given template and returns the new template.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GenerateDynamicContentHtmlAsync() in this method.</param>
    /// <param name="template">The template to replace the dynamic content in.</param>
    /// <param name="componentOverrides">Optional: If you already have the settings for one or more components, you can add them here. This is made to he </param>
    /// <returns>The new template with all dynamic content.</returns>
    Task<string> ReplaceAllDynamicContentAsync(ITemplatesService templatesService, string template, List<DynamicContent> componentOverrides = null);

    /// <summary>
    /// Executes a query and converts the results into an JSON object.
    /// </summary>
    /// <param name="queryTemplate">The query to execute with the grouping settings for if and how to group the results in the JSON.</param>
    /// <param name="encryptionKey">Optional: The key to encrypt/decrypt values in the results. Default is the key from the app settings.</param>
    /// <param name="skipNullValues">Optional: Whether to skip values that are <see langword="null"/> and not add them to the JSON. Default value is <see langword="false"/>.</param>
    /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
    /// <param name="recursive">TODO</param>
    /// <param name="childItemsMustHaveId">Optional: Forces child items in an object to have a non-null value in the <c>id</c> column. This is for data selectors that have optional child items.</param>
    /// <returns>A <see cref="JArray"/> with the results of the query.</returns>
    Task<JArray> GetJsonResponseFromQueryAsync(QueryTemplate queryTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false, bool recursive = false, bool childItemsMustHaveId = false);

    /// <summary>
    /// Executes a query and converts the results into an JSON object.
    /// </summary>
    /// <param name="routineTemplate">The routine to execute.</param>
    /// <param name="encryptionKey">Optional: The key to encrypt/decrypt values in the results. Default is the key from the app settings.</param>
    /// <param name="skipNullValues">Optional: Whether to skip values that are <see langword="null"/> and not add them to the JSON. Default value is <see langword="false"/>.</param>
    /// <param name="allowValueDecryption">Optional: Set to <see langword="true"/> to allow values to be decrypted (for columns that contain the _decrypt suffix for example), otherwise values will be added in the <see cref="JObject"/> as is. Default value is <see langword="false"/>.</param>
    /// <returns>A <see cref="JArray"/> with the results of the query.</returns>
    Task<JArray> GetJsonResponseFromRoutineAsync(RoutineTemplate routineTemplate, string encryptionKey = null, bool skipNullValues = false, bool allowValueDecryption = false);

    /// <summary>
    /// Get an HTML template together with the linked css and javascript.
    /// Must supply either ID or name of the template.
    /// </summary>
    /// <param name="id">Optional: The ID of the template.</param>
    /// <param name="name">Optional: The name of the template.</param>
    /// <param name="parentId">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <param name="parentName">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <returns>A <see cref="TemplateDataModel"/> with the HTML template, the linked CSS and the linked javascript.</returns>
    Task<TemplateDataModel> GetTemplateDataAsync(int id = 0, string name = "", int parentId = 0, string parentName = "");

    /// <summary>
    /// Get an HTML template together with the linked css and javascript.
    /// Must supply either ID or name of the template.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetTemplateAsync() in this method.</param>
    /// <param name="id">Optional: The ID of the template.</param>
    /// <param name="name">Optional: The name of the template.</param>
    /// <param name="parentId">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <param name="parentName">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <returns>A <see cref="TemplateDataModel"/> with the HTML template, the linked CSS and the linked javascript.</returns>
    Task<TemplateDataModel> GetTemplateDataAsync(ITemplatesService templatesService, int id = 0, string name = "", int parentId = 0, string parentName = "");

    /// <summary>
    /// Executes the preload query for an HTML template, if it's set. After executing it, it will save the first <see cref="DataRow"/> of the results in the HttpContext.
    /// </summary>
    /// <param name="template">The template with a preload query to execute.</param>
    /// <returns><see langword="true"/> if there is no query to execute or if the query returned 1 or more results, <see langword="false"/> if there is a query, but it returned 0 results.</returns>
    Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(Template template);

    /// <summary>
    /// Executes the preload query for an HTML template, if it's set. After executing it, it will save the first <see cref="DataRow"/> of the results in the HttpContext.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetTemplateAsync() in this method.</param>
    /// <param name="template">The template with a preload query to execute.</param>
    /// <returns><see langword="true"/> if there is no query to execute or if the query returned 1 or more results, <see langword="false"/> if there is a query, but it returned 0 results.</returns>
    Task<bool> ExecutePreLoadQueryAndRememberResultsAsync(ITemplatesService templatesService, Template template);

    /// <summary>
    /// Creates the file name the cached HTML will be saved to and loaded from.
    /// </summary>
    /// <param name="contentTemplate">The <see cref="Template"/>.</param>
    /// <param name="extension">Optional: The extension to use for the file name. Default is ".html".</param>
    /// <param name="useAbsoluteImageUrls">Whether to force all URLs for images to be absolute. This will add the main domain from the settings to all image URLs that are not absolute yet.</param>
    /// <param name="removeSvgUrlsFromIcons">Whether to remove SVG URLs from all icons. If true, this removes the URLs from SVG files to allow the template to load SVGs when the HTML is placed inside another website. To use this functionality, the content of the SVG needs to be placed in the HTML, xlink can only load URLs from the same domain, protocol and port.</param>
    /// <returns>The name for the file to cache the contents of the template to.</returns>
    Task<string> GetTemplateOutputCacheFileNameAsync(Template contentTemplate, string extension = ".html", bool useAbsoluteImageUrls = false, bool removeSvgUrlsFromIcons = false);

    /// <summary>
    /// Gets all templates that have a URL regex setup.
    /// </summary>
    /// <returns>A list with all templates that have a URL regex set, but only the regex, ID and type of each template.</returns>
    Task<List<Template>> GetTemplateUrlsAsync();

    /// <summary>
    /// Get whether the rendering of a specific component should be logged.
    /// </summary>
    /// <param name="componentId">The ID of the component to check for.</param>
    /// <returns>A boolean indicating whether the rendering of this component should be logged.</returns>
    Task<bool> ComponentRenderingShouldBeLoggedAsync(int componentId);

    /// <summary>
    /// Get whether the rendering of a specific template should be logged.
    /// </summary>
    /// <param name="templateId">The ID of the template to check for.</param>
    /// <returns>A boolean indicating whether the rendering of this template should be logged.</returns>
    Task<bool> TemplateRenderingShouldBeLoggedAsync(int templateId);

    /// <summary>
    /// Adds a row to the log table for keeping track of when components and templates are being rendered and how long it takes every time.
    /// </summary>
    /// <param name="componentId">The ID of the component. Set to 0 if you're adding a log for a template.</param>
    /// <param name="templateId">The ID of the template. Set to 0 if you're rendering a component.</param>
    /// <param name="version">The version of the component or template.</param>
    /// <param name="startTime">The date and time that the rendering started.</param>
    /// <param name="endTime">The date and time that the rendering was finished.</param>
    /// <param name="timeTaken">The amount of time, in milliseconds, that it took to render the component or template.</param>
    /// <param name="error">Optional: If an error occurred, put that error here.</param>
    Task AddTemplateOrComponentRenderingLogAsync(int componentId, int templateId, int version, DateTime startTime, DateTime endTime, long timeTaken, string error = "");

    /// <summary>
    /// Gets all custom HTML snippets that should be loaded on all pages.
    /// This will return all HTML snippets in the order that they should be loaded.
    /// </summary>
    /// <returns>A list of HTML snippets for the given template, in the order that they should be added to the page.</returns>
    Task<List<PageWidgetModel>> GetGlobalPageWidgetsAsync();

    /// <summary>
    /// Gets all custom HTML snippets for a template. By default, this will also include snippets that are added globally, to load on all pages.
    /// This will return all HTML snippets in the order that they should be loaded. Global widgets will always be added first, then template specific widgets.
    /// </summary>
    /// <param name="templateId">The ID of the template to load the snippets for.</param>
    /// <param name="includeGlobalSnippets">Optional: Whether to include global snippets that are added for all pages. Default is <see langword="true"/>.</param>
    /// <returns>A list of HTML snippets for the given template, in the order that they should be added to the page.</returns>
    Task<List<PageWidgetModel>> GetPageWidgetsAsync(int templateId, bool includeGlobalSnippets = true);

    /// <summary>
    /// Gets all custom HTML snippets for a template. By default, this will also include snippets that are added globally, to load on all pages.
    /// This will return all HTML snippets in the order that they should be loaded. Global widgets will always be added first, then template specific widgets.
    /// </summary>
    /// <param name="templatesService">The <see cref="ITemplatesService"/> to use, to prevent duplicate code while using caching with the decorator pattern, while still being able to use caching in calls to GetGlobalPageWidgetsAsync() in this method.</param>
    /// <param name="templateId">The ID of the template to load the snippets for.</param>
    /// <param name="includeGlobalSnippets">Optional: Whether to include global snippets that are added for all pages. Default is <see langword="true"/>.</param>
    /// <returns>A list of HTML snippets for the given template, in the order that they should be added to the page.</returns>
    Task<List<PageWidgetModel>> GetPageWidgetsAsync(ITemplatesService templatesService, int templateId, bool includeGlobalSnippets = true);

    /// <summary>
    /// Checks the permissions of the template and returns an empty template with an ID of 0, if user does not have permission
    /// This result and check is the same GetTemplateAsync does if permissions are not skipped
    /// </summary>
    /// <param name="template">The template to check permissions on.</param>
    /// <remarks>You can GetTemplatePermissionSettingsAsync to get a template model with only the relevant properties filled.</remarks>
    /// <returns>Returns the given template if user has permissions or empty template with an ID of 0 if they do not.</returns>
    Task<Template> CheckTemplatePermissionsAsync(Template template);

    /// <summary>
    /// Gets a template model with only the permissions settings filled
    /// </summary>
    /// <param name="id">Optional: The ID of the template.</param>
    /// <param name="name">Optional: The name of the template.</param>
    /// <param name="parentId">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <param name="parentName">Optional: The ID of the parent directory, in case you only want to search in a specific directory when looking up a template by name.</param>
    /// <returns>Template model with only the permissions settings filled</returns>
    Task<Template> GetTemplatePermissionSettingsAsync(int id = 0, string name = "", int parentId = 0, string parentName = "");
}