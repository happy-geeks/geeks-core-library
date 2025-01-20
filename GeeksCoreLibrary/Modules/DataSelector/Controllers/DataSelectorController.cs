using System;
using System.Net;
using System.Threading.Tasks;
using GeeksCoreLibrary.Modules.DataSelector.Interfaces;
using GeeksCoreLibrary.Modules.DataSelector.Models;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Modules.DataSelector.Controllers;

[Area("DataSelector")]
public class DataSelectorController(IDataSelectorsService dataSelectorsService) : Controller
{
    [IgnoreAntiforgeryToken]
    [Route("/GetItems.gcl")]
    [Route("/get_items.gcl")]
    [Route("/get_items.jcl")]
    [HttpGet]
    public async Task<IActionResult> GetItems([FromQuery] DataSelectorRequestModel data, [FromQuery] string encryptedWiser2UserId = null)
    {
        if (!data.DataSelectorId.HasValue && !String.IsNullOrWhiteSpace(data.QueryId))
        {
            return BadRequest();
        }

        var (result, statusCode, error) = await dataSelectorsService.GetJsonResponseAsync(data);

        if (statusCode != HttpStatusCode.OK)
        {
            return StatusCode((int) statusCode, error);
        }

        return Json(result);
    }

    /// <summary>
    /// Get items using a POST request, which has the data selector model in the post body.
    /// </summary>
    /// <param name="dataFromBody"></param>
    /// <param name="dataFromUri"></param>
    /// <returns></returns>
    [Route("/GetItems.gcl")]
    [Route("/get_items.gcl")]
    [Route("/get_items.jcl")]
    [HttpPost]
    public async Task<IActionResult> GetItems([FromBody] DataSelectorRequestModel dataFromBody, [FromQuery] DataSelectorRequestModel dataFromUri)
    {
        if (dataFromBody == null && dataFromUri == null)
        {
            return BadRequest();
        }

        var data = CombineDataSelectorRequestModels(dataFromBody, dataFromUri);

        var (result, statusCode, error) = await dataSelectorsService.GetJsonResponseAsync(data);

        if (statusCode != HttpStatusCode.OK)
        {
            return StatusCode((int) statusCode, error);
        }

        if (data.ToExcel.HasValue && data.ToExcel.Value)
        {
            FileContentResult excelResult;
            (excelResult, statusCode, error) = await dataSelectorsService.ToExcelAsync(data);

            if (statusCode != HttpStatusCode.OK)
            {
                return StatusCode((int) statusCode, error);
            }

            return File(excelResult.FileContents, excelResult.ContentType);
        }

        if (!String.IsNullOrWhiteSpace(data.OutputTemplate) || !String.IsNullOrWhiteSpace(data.ContentItemId))
        {
            FileContentResult pdfResult;
            (pdfResult, statusCode, error) = await dataSelectorsService.ToPdfAsync(data);

            if (statusCode != HttpStatusCode.OK)
            {
                return StatusCode((int) statusCode, error);
            }

            return File(pdfResult.FileContents, pdfResult.ContentType);
        }

        return Json(result);
    }

    /// <summary>
    /// Combines two models into one. Values in <paramref name="model1"/> are only overwritten by the values in <paramref name="model2"/> if they're null or empty.
    /// </summary>
    /// <param name="model1">Main <see cref="DataSelectorRequestModel"/> object.</param>
    /// <param name="model2">Second <see cref="DataSelectorRequestModel"/> object to supplement values missing in the main model.</param>
    /// <returns></returns>
    private static DataSelectorRequestModel CombineDataSelectorRequestModels(DataSelectorRequestModel model1, DataSelectorRequestModel model2)
    {
        if (model1 == null)
        {
            return model2;
        }

        if (model2 == null)
        {
            return model1;
        }

        model1.Settings ??= model2.Settings;
        model1.Descendants ??= model2.Descendants;
        model1.ExtraData ??= model2.ExtraData;
        model1.DataSelectorId ??= model2.DataSelectorId;
        if (String.IsNullOrWhiteSpace(model1.QueryId)) model1.QueryId = model2.QueryId;
        if (String.IsNullOrWhiteSpace(model1.ModuleId)) model1.ModuleId = model2.ModuleId;
        model1.NumberOfLevels ??= model2.NumberOfLevels;
        if (String.IsNullOrWhiteSpace(model1.LanguageCode)) model1.LanguageCode = model2.LanguageCode;
        if (String.IsNullOrWhiteSpace(model1.NumberOfItems)) model1.NumberOfItems = model2.NumberOfItems;
        model1.PageNumber ??= model2.PageNumber;
        if (String.IsNullOrWhiteSpace(model1.ContainsPath)) model1.ContainsPath = model2.ContainsPath;
        if (String.IsNullOrWhiteSpace(model1.ContainsPath)) model1.ContainsPath = model2.ContainsPath;
        if (String.IsNullOrWhiteSpace(model1.ParentId)) model1.ParentId = model2.ParentId;
        if (String.IsNullOrWhiteSpace(model1.EntityTypes)) model1.EntityTypes = model2.EntityTypes;
        model1.LinkType ??= model2.LinkType;
        if (String.IsNullOrWhiteSpace(model1.QueryAddition)) model1.QueryAddition = model2.QueryAddition;
        if (String.IsNullOrWhiteSpace(model1.OrderPart)) model1.OrderPart = model2.OrderPart;
        if (String.IsNullOrWhiteSpace(model1.Fields)) model1.Fields = model2.Fields;
        if (String.IsNullOrWhiteSpace(model1.FileTypes)) model1.FileTypes = model2.FileTypes;
        if (String.IsNullOrWhiteSpace(model1.FileName)) model1.FileName = model2.FileName;
        if (String.IsNullOrWhiteSpace(model1.OutputTemplate)) model1.OutputTemplate = model2.OutputTemplate;
        if (String.IsNullOrWhiteSpace(model1.ContentItemId)) model1.ContentItemId = model2.ContentItemId;
        if (String.IsNullOrWhiteSpace(model1.ContentPropertyName)) model1.ContentPropertyName = model2.ContentPropertyName;
        model1.DateTime ??= model2.DateTime;
        if (String.IsNullOrWhiteSpace(model1.Hash)) model1.Hash = model2.Hash;
        return model1;
    }
}