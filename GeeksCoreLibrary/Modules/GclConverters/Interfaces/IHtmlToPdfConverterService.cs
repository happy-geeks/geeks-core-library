using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.GclConverters.Interfaces;

/// <summary>
/// A service for converting HTML to PDF files, with settings that can be stored in the database, or passed as a parameter.
/// </summary>
public interface IHtmlToPdfConverterService
{
    /// <summary>
    /// Convert HTML to a PDF.
    /// </summary>
    /// <param name="settings">The HTML and PDF settings.</param>
    /// <returns>A FileContentResult that can be used in a controller.</returns>
    Task<FileContentResult> ConvertHtmlStringToPdfAsync(HtmlToPdfRequestModel settings);

    /// <summary>
    /// Make sure that the file name is not empty, contains no invalid characters and has the extension ".pdf".
    /// </summary>
    /// <param name="input">The file name to check.</param>
    /// <returns>The new and correct file name</returns>
    string EnsureCorrectFileName(string input);

    /// <summary>
    /// Gets the settings for converting HTML to PDF from a template in Wiser.
    /// </summary>
    /// <param name="templateItemId">The ID of the template entity that contains the settings.</param>
    /// <param name="languageCode">Optional: For a multi language site/template, enter the language code you want the settings of here.</param>
    /// <param name="contentPropertyName">Optional: If you have a template entity where the HTML content/template is saved in a different property than <see cref="HtmlTemplateConstants.HtmlTemplatePropertyName"/>, you can enter that here.</param>
    /// <returns>The <see cref="HtmlToPdfRequestModel"/> with the settings.</returns>
    Task<HtmlToPdfRequestModel> GetHtmlToPdfSettingsAsync(ulong templateItemId, string languageCode = null, string contentPropertyName = null);

    /// <summary>
    /// Retrieve the background image for the PDF from Wiser.
    /// </summary>
    /// <param name="itemId"></param>
    /// <param name="backgroundPropertyName"></param>
    /// <returns></returns>
    Task<string> RetrieveBackgroundImageAsync(ulong itemId, string backgroundPropertyName);
}