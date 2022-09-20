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

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [HttpGet]
        public async Task<IActionResult> WiserItemImage(int wiserVersion, ulong itemId, string propertyName, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, int fileNumber = 1, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', preferredWidth: '{preferredWidth}', preferredHeight: '{preferredHeight}', filename: '{filename}', resizeMode: '{resizeMode:G}', anchorPosition: '{anchorPosition}', fileNumber: '{fileNumber}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserItemImageAsync(itemId, propertyName, preferredWidth, preferredHeight, filename, fileNumber, resizeMode, anchorPosition, encryptedId);

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, FileSystemHelpers.GetMediaTypeByMagicNumber(fileBytes));
        }

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]

        [HttpGet]
        public async Task<IActionResult> WiserItemLinkImage(int wiserVersion, ulong itemLinkId, string propertyName, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, int fileNumber = 1, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, itemId: '{itemLinkId}', propertyName: '{propertyName}', preferredWidth: '{preferredWidth}', preferredHeight: '{preferredHeight}', filename: '{filename}', resizeMode: '{resizeMode:G}', anchorPosition: '{anchorPosition}', fileNumber: '{fileNumber}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserItemLinkImageAsync(itemLinkId, propertyName, preferredWidth, preferredHeight, filename, fileNumber, resizeMode, anchorPosition, encryptedId);

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, FileSystemHelpers.GetMediaTypeByMagicNumber(fileBytes));
        }

        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/global_file/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/global_file/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/{resizeMode}-{anchorPosition}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/{resizeMode}/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]
        [Route("/image/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/{preferredWidth:int:range(0, 5000)}/{preferredHeight:int:range(0, 5000)}/{filename:regex(.+?\\..+?)}")]
        [HttpGet]
        public async Task<IActionResult> WiserDirectImage(int wiserVersion, ulong itemFileId, int preferredWidth, int preferredHeight, string filename, ResizeModes resizeMode = ResizeModes.Normal, AnchorPositions anchorPosition = AnchorPositions.Center, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, fileId: '{itemFileId}', preferredWidth: '{preferredWidth}', preferredHeight: '{preferredHeight}', filename: '{filename}', resizeMode: '{resizeMode:G}', anchorPosition: '{anchorPosition}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserDirectImageAsync(itemFileId, preferredWidth, preferredHeight, filename, resizeMode, anchorPosition, encryptedId);

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, FileSystemHelpers.GetMediaTypeByMagicNumber(fileBytes));
        }

        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemId}/{propertyName}/{filename:regex(.+?\\..+?)}")]
        [HttpGet]
        public async Task<IActionResult> WiserItemFile(int wiserVersion, ulong itemId, string propertyName, string filename, int fileNumber = 1, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, itemId: '{itemId}', propertyName: '{propertyName}', filename: '{filename}', fileNumber: '{fileNumber}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserItemFileAsync(itemId, propertyName, filename, fileNumber, encryptedId);

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, MediaTypeNames.Application.Octet);
        }

        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{fileNumber:int}/{filename:regex(.+?\\..+?)}")]
        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemLinkId}/itemlink/{propertyName}/{filename:regex(.+?\\..+?)}")]
        [HttpGet]
        public async Task<IActionResult> WiserItemLinkFile(int wiserVersion, ulong itemLinkId, string propertyName, string filename, int fileNumber = 1, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, itemId: '{itemLinkId}', propertyName: '{propertyName}', filename: '{filename}', fileNumber: '{fileNumber}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserItemLinkFileAsync(itemLinkId, propertyName, filename, fileNumber, encryptedId);

            if (fileBytes == null)
            {
                return NotFound();
            }

            Response.Headers.Add("Last-Modified", lastModified.ToString("R"));
            Response.Headers.Add("Expires", lastModified.Add(gclSettings.DefaultItemFileCacheDuration).ToString("R"));
            return File(fileBytes, MediaTypeNames.Application.Octet);
        }

        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/global_file/{filename:regex(.+?\\..+?)}")]
        [Route("/file/wiser{wiserVersion:range(1,3)}/{itemFileId}/direct/{filename:regex(.+?\\..+?)}")]
        [HttpGet]
        public async Task<IActionResult> WiserDirectFile(int wiserVersion, ulong itemFileId, string filename, [FromQuery] string encryptedId = null)
        {
            logger.LogDebug($"Get image from Wiser, itemId: '{itemFileId}', filename: '{filename}'");
            var (fileBytes, lastModified) = await itemFilesService.GetWiserDirectFileAsync(itemFileId, filename, encryptedId);

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