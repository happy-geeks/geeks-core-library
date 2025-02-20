using System;
using System.Net.Http.Headers;
using System.Net.Mime;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.ItemFiles.Controllers;

[Area("ItemFiles")]
public class ItemFilesController(ILogger<ItemFilesController> logger, IOptions<GclSettings> gclSettings, IItemFilesService itemFilesService)
    : Controller
{
    private readonly GclSettings gclSettings = gclSettings.Value;

    [Route("/wiser-image.gcl")]
    [HttpGet]
    public async Task<IActionResult> WiserItemImage([FromQuery] ulong itemId, [FromQuery] string propertyName,
        [FromQuery] string fileName, [FromQuery] uint preferredWidth = 0, [FromQuery] uint preferredHeight = 0,
        [FromQuery] ResizeModes resizeMode = ResizeModes.Normal,
        [FromQuery] AnchorPositions anchorPosition = AnchorPositions.Center, [FromQuery] int fileNumber = 0,
        [FromQuery] string fileType = null, [FromQuery] string type = null,
        [FromQuery] string encryptedId = null)
    {
        if ((itemId == 0 && String.IsNullOrEmpty(encryptedId)) || String.IsNullOrWhiteSpace(propertyName))
        {
            return NotFound();
        }

        // Also check if fileName is empty when fileType is "name"
        if (String.Equals(fileType, "name", StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(fileName))
        {
            return NotFound();
        }

        // Get cache control from client.
        if (!CacheControlHeaderValue.TryParse(Request.Headers.CacheControl, out var cacheControl))
        {
            cacheControl = new CacheControlHeaderValue();
        }

        logger.LogDebug($"Get image from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', preferredWidth: '{preferredWidth}', preferredHeight: '{preferredHeight}', filename: '{fileName}', resizeMode: '{resizeMode:G}', anchorPosition: '{anchorPosition}', fileNumber: '{fileNumber}'");

        FileResultModel fileResult;
        switch (fileType?.ToUpperInvariant())
        {
            case "ITEMLINK":
                _ = Int32.TryParse(type, out var linkType);
                fileResult = await itemFilesService.GetWiserItemLinkImageAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber, resizeMode, anchorPosition, encryptedId, linkType);
                break;
            case "DIRECT":
                fileResult = await itemFilesService.GetWiserDirectImageAsync(itemId, preferredWidth, preferredHeight, fileName, resizeMode, anchorPosition, encryptedId, entityType: type);
                break;
            case "NAME":
                fileResult = await itemFilesService.GetWiserImageByFileNameAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, resizeMode, anchorPosition, encryptedId, entityType: type);
                break;
            default:
                fileResult = await itemFilesService.GetWiserItemImageAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber, resizeMode, anchorPosition, encryptedId, entityType: type);
                break;
        }

        if (fileResult?.FileBytes == null || fileResult.FileBytes.Length == 0)
        {
            return NotFound();
        }

        // Set cache headers to tell browsers and CDNs how to cache the image. Do not cache protected files, because they can contain sensitive data.
        Response.Headers.CacheControl = fileResult.WiserItemFile.Protected || cacheControl.NoStore ? "no-store" : $"public, max-age={gclSettings.DefaultItemFileCacheDuration.TotalSeconds}";
        Response.Headers.LastModified = fileResult.LastModified.ToString("R");
        Response.Headers.Expires = fileResult.LastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R");

        var contentType = FileSystemHelpers.GetContentTypeOfImage(fileResult.WiserItemFile.FileName, fileResult.FileBytes, fileResult.WiserItemFile.ContentType);
        return File(fileResult.FileBytes, contentType);
    }

    [Route("wiser-file.gcl")]
    [HttpGet]
    public async Task<IActionResult> WiserItemFile([FromQuery] ulong itemId, [FromQuery] string propertyName,
        [FromQuery] string filename, [FromQuery] int fileNumber = 1, [FromQuery] string encryptedId = null,
        [FromQuery] string type = null, [FromQuery] string fileType = null)
    {
        if ((itemId == 0 && String.IsNullOrEmpty(encryptedId)) || String.IsNullOrWhiteSpace(propertyName))
        {
            return NotFound();
        }

        // Get cache control from client.
        if (!CacheControlHeaderValue.TryParse(Request.Headers.CacheControl, out var cacheControl))
        {
            cacheControl = new CacheControlHeaderValue();
        }

        logger.LogDebug($"Get file from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', filename: '{filename}', fileNumber: '{fileNumber}'");

        FileResultModel fileResult;
        switch (fileType?.ToUpperInvariant())
        {
            case "ITEMLINK":
                _ = Int32.TryParse(type, out var linkType);
                fileResult = await itemFilesService.GetWiserItemLinkFileAsync(itemId, propertyName, filename, fileNumber, encryptedId, linkType);
                break;
            case "DIRECT":
                fileResult = await itemFilesService.GetWiserDirectFileAsync(itemId, filename, encryptedId, entityType: type);
                break;
            default:
                fileResult = await itemFilesService.GetWiserItemFileAsync(itemId, propertyName, filename, fileNumber, encryptedId, entityType: type);
                break;
        }

        if (fileResult?.FileBytes == null || fileResult.FileBytes.Length == 0)
        {
            return NotFound();
        }

        // Set cache headers to tell browsers and CDNs how to cache the image. Do not cache protected files, because they can contain sensitive data.
        Response.Headers.CacheControl = fileResult.WiserItemFile.Protected || cacheControl.NoStore ? "no-store" : $"public, max-age={gclSettings.DefaultItemFileCacheDuration.TotalSeconds}";
        Response.Headers.LastModified = fileResult.LastModified.ToString("R");
        Response.Headers.Expires = fileResult.LastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R");
        return File(fileResult.FileBytes, MediaTypeNames.Application.Octet);
    }
}