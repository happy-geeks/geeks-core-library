using System;
using System.Net.Mime;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.ItemFiles.Enums;
using GeeksCoreLibrary.Modules.ItemFiles.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.ItemFiles.Controllers
{
    [Area("ItemFiles")]
    public class ItemFilesController : Controller
    {
        private readonly ILogger<ItemFilesController> logger;
        private readonly GclSettings gclSettings;
        private readonly IItemFilesService itemFilesService;

        public ItemFilesController(ILogger<ItemFilesController> logger, IOptions<GclSettings> gclSettings, IItemFilesService itemFilesService)
        {
            this.logger = logger;
            this.gclSettings = gclSettings.Value;
            this.itemFilesService = itemFilesService;
        }
        
        [Route("/wiser-image.gcl")]
        [HttpGet]
        public async Task<IActionResult> WiserItemImage([FromQuery] ulong itemId, [FromQuery] string propertyName,
            [FromQuery] string fileName, [FromQuery] int preferredWidth = 0, [FromQuery] int preferredHeight = 0,
            [FromQuery] ResizeModes resizeMode = ResizeModes.Normal,
            [FromQuery] AnchorPositions anchorPosition = AnchorPositions.Center, [FromQuery] int fileNumber = 0,
            [FromQuery] string fileType = null, [FromQuery] string type = null,
            [FromQuery] string encryptedId = null)
        {
            if (itemId == 0 || String.IsNullOrWhiteSpace(propertyName))
            {
                return NotFound();
            }
            // Also check if fileName is empty when fileType is "name"
            if (String.Equals(fileType, "name", StringComparison.OrdinalIgnoreCase) && String.IsNullOrWhiteSpace(fileName))
            {
                return NotFound();
            }

            logger.LogDebug($"Get image from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', preferredWidth: '{preferredWidth}', preferredHeight: '{preferredHeight}', filename: '{fileName}', resizeMode: '{resizeMode:G}', anchorPosition: '{anchorPosition}', fileNumber: '{fileNumber}'");

            byte[] fileBytes;
            DateTime lastModified;

            switch (fileType?.ToUpperInvariant())
            {
                case "ITEMLINK":
                    Int32.TryParse(type, out var linkType);
                    (fileBytes, lastModified) = await itemFilesService.GetWiserItemLinkImageAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber, resizeMode, anchorPosition, encryptedId, linkType);
                    break;
                case "DIRECT":
                    (fileBytes, lastModified) = await itemFilesService.GetWiserDirectImageAsync(itemId, preferredWidth, preferredHeight, fileName, resizeMode, anchorPosition, encryptedId, entityType: type);
                    break;
                case "NAME":
                    (fileBytes, lastModified) = await itemFilesService.GetWiserImageByFileNameAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, resizeMode, anchorPosition, encryptedId, entityType: type);
                    break;
                default:
                    (fileBytes, lastModified) = await itemFilesService.GetWiserItemImageAsync(itemId, propertyName, preferredWidth, preferredHeight, fileName, fileNumber, resizeMode, anchorPosition, encryptedId, entityType: type);
                    break;
            }

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, FileSystemHelpers.GetMediaTypeByMagicNumber(fileBytes));
        }

        [Route("wiser-file.gcl")]
        [HttpGet]
        public async Task<IActionResult> WiserItemFile([FromQuery] ulong itemId, [FromQuery] string propertyName,
            [FromQuery] string filename, [FromQuery] int fileNumber = 1, [FromQuery] string encryptedId = null,
            [FromQuery] string type = null, [FromQuery] string fileType = null)
        {
            if (itemId == 0 || String.IsNullOrWhiteSpace(propertyName))
            {
                return NotFound();
            }
            
            logger.LogDebug($"Get file from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', filename: '{filename}', fileNumber: '{fileNumber}'");
            byte[] fileBytes;
            DateTime lastModified;

            switch (fileType?.ToUpperInvariant())
            {
                case "ITEMLINK":
                    Int32.TryParse(type, out var linkType);
                    (fileBytes, lastModified) = await itemFilesService.GetWiserItemLinkFileAsync(itemId, propertyName, filename, fileNumber, encryptedId, linkType);
                    break;
                case "DIRECT":
                    (fileBytes, lastModified) = await itemFilesService.GetWiserDirectFileAsync(itemId, filename, encryptedId, entityType: type);
                    break;
                default:
                    (fileBytes, lastModified) = await itemFilesService.GetWiserItemFileAsync(itemId, propertyName, filename, fileNumber, encryptedId, entityType: type);
                    break;
            }

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, MediaTypeNames.Application.Octet);
        }
    }
}