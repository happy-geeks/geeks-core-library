using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.GclConverters.Interfaces
{
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
    }
}