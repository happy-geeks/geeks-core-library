using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Interfaces;
using GeeksCoreLibrary.Components.ShoppingBasket.Models;
using GeeksCoreLibrary.Core.Cms;
using GeeksCoreLibrary.Core.Cms.Attributes;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Communication.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Interfaces;
using GeeksCoreLibrary.Modules.GclConverters.Models;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Models;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Html;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Components.ShoppingBasket
{
    [CmsObject(
        PrettyName = "Shopping Basket",
        Description = "Component for handling shopping baskets on a website, such as rendering the basket, adding products, removing products, etc."
    )]
    public class ShoppingBasket : CmsComponent<ShoppingBasketCmsSettingsModel, ShoppingBasket.ComponentModes>
    {
        private readonly IShoppingBasketsService shoppingBasketsService;
        private readonly IWebHostEnvironment webHostEnvironment;
        private readonly IObjectsService objectsService;
        private readonly IHtmlToPdfConverterService htmlToPdfConverterService;
        private readonly ICommunicationsService communicationsService;

        // Some temporary variables.
        private string basketLineValidityMessage;
        private string basketLineStockActionMessage;

        #region Enums

        public enum ComponentModes
        {
            /// <summary>
            /// For rendering the shopping basket on a web page.
            /// </summary>
            Render = 1,

            /// <summary>
            /// For adding one or more items to the shopping basket, or increasing its quantity if the item is already in the shopping basket.
            /// </summary>
            [CmsEnum(PrettyName = "Add items")]
            AddItems = 2,

            /// <summary>
            /// For updating an item in the basket. This requires the ID of the basket line that should be updated/replaced to be present in the request.
            /// </summary>
            [CmsEnum(PrettyName = "Update item")]
            UpdateItem = 11,

            /// <summary>
            /// For changing the quantity of one or more items in the shopping basket.
            /// </summary>
            [CmsEnum(PrettyName = "Change quantity")]
            ChangeQuantity = 3,

            /// <summary>
            /// For removing an item from the shopping basket.
            /// </summary>
            [CmsEnum(PrettyName = "Remove items")]
            RemoveItems = 4,

            /// <summary>
            /// For adding a coupon to the shopping basket.
            /// </summary>
            [CmsEnum(PrettyName = "Add coupon")]
            AddCoupon = 12,

            /// <summary>
            /// For clearing the basket's contents, but keeping the basket.
            /// </summary>
            [CmsEnum(PrettyName = "Clear contents")]
            ClearContents = 5,

            /// <summary>
            /// For completely resetting the shopping basket. This will create a new basket.
            /// </summary>
            Reset = 6,

            /// <summary>
            /// For rendering the basket for a print layout.
            /// </summary>
            Print = 7,

            /// <summary>
            /// For generating a PDF file based on the basket HTML.
            /// </summary>
            [CmsEnum(PrettyName = "Generate PDF")]
            GeneratePdf = 8,

            /// <summary>
            /// For emailing the basket.
            /// </summary>
            Email = 9,

            /// <summary>
            /// For JCL baskets that should run with the GCL code.
            /// </summary>
            [CmsEnum(HideInCms = true)]
            Legacy = 10
        }

        public enum PriceTypes
        {
            InVatInDiscount,
            InVatExDiscount,
            ExVatInDiscount,
            ExVatExDiscount,
            VatOnly,
            DiscountInVat,
            DiscountExVat,
            PspPriceInVat,
            PspPriceExVat
        }

        /// <summary>
        /// The legacy price types have the names the JCL gave them.
        /// </summary>
        [SuppressMessage("ReSharper", "InconsistentNaming")]
        public enum LegacyPriceTypes
        {
            In_VAT_In_Discount,
            In_VAT_Ex_Discount,
            Ex_VAT_In_Discount,
            Ex_VAT_Ex_Discount,
            VAT,
            Discount_In_VAT,
            Discount_Ex_VAT,
            PspPrice_In_VAT,
            PspPrice_Ex_Vat
        }

        public enum HandleCouponResults
        {
            /// <summary>
            /// Coupon code was accepted and coupon has been added to the shopping basket.
            /// </summary>
            CouponAccepted = 1,

            /// <summary>
            /// An existing coupon's discount has been updated.
            /// </summary>
            CouponDiscountUpdated = 2,

            /// <summary>
            /// The coupon code was empty or invalid.
            /// </summary>
            InvalidCouponCode = 3,

            /// <summary>
            /// A coupon with a given coupon code was already added to the shopping basket.
            /// </summary>
            CouponAlreadyAdded = 4,

            /// <summary>
            /// When the maximum amount of coupons is already added to the shopping basket.
            /// </summary>
            MaximumCouponsReached = 5,

            /// <summary>
            /// Coupon couldn't be saved due to the HTTP context being unavailable.
            /// </summary>
            HttpContextUnavailable = 6
        }

        #endregion

        #region Properties

        public WiserItemModel Main { get; set; }

        public List<WiserItemModel> Lines { get; set; }

        #endregion

        #region Constructor

        public ShoppingBasket()
        {
            Settings = new ShoppingBasketCmsSettingsModel();

            Main = new WiserItemModel();
            Lines = new List<WiserItemModel>();
        }

        [ActivatorUtilitiesConstructor]
        public ShoppingBasket(ILogger<ShoppingBasket> logger, IDatabaseConnection databaseConnection, IShoppingBasketsService shoppingBasketsService, ITemplatesService templatesService, IWebHostEnvironment webHostEnvironment, IStringReplacementsService stringReplacementsService, IObjectsService objectsService, IAccountsService accountsService, IHtmlToPdfConverterService htmlToPdfConverterService, ICommunicationsService communicationsService)
        {
            this.shoppingBasketsService = shoppingBasketsService;
            this.webHostEnvironment = webHostEnvironment;
            this.objectsService = objectsService;
            this.htmlToPdfConverterService = htmlToPdfConverterService;
            this.communicationsService = communicationsService;

            Logger = logger;
            StringReplacementsService = stringReplacementsService;
            DatabaseConnection = databaseConnection;
            TemplatesService = templatesService;
            AccountsService = accountsService;

            Settings = new ShoppingBasketCmsSettingsModel();

            Main = new WiserItemModel();
            Lines = new List<WiserItemModel>();
        }

        #endregion

        #region Manipulating basket contents (adding/removing/etc.)

        public WiserItemModel GetLine(ulong id)
        {
            if (Lines == null || !Lines.Any())
            {
                return null;
            }

            return Lines.FirstOrDefault(line => line != null && line.Id == id);
        }

        /// <summary>
        /// Get lines of a specific type.
        /// </summary>
        /// <param name="lineType">The type of lines to look for.</param>
        /// <returns>A <see cref="List{T}"/> of <see cref="WiserItemModel"/> objects that represent the order lines of the given type.</returns>
        public List<WiserItemModel> GetLines(string lineType)
        {
            if (Lines == null)
            {
                Lines = new List<WiserItemModel>();
                return new List<WiserItemModel>();
            }

            if (!Lines.Any() || String.IsNullOrWhiteSpace(lineType))
            {
                return Lines;
            }

            return Lines.Where(line => line != null && line.GetDetailValue("type").Equals(lineType, StringComparison.OrdinalIgnoreCase)).ToList();
        }

        public async Task ResetAsync()
        {
            // First delete the basket from database.
            await shoppingBasketsService.DeleteAsync(Main.Id);

            // Then delete the basket cookie.
            HttpContext.Response.Cookies.Delete(Settings.CookieName);

            // And finally create a new basket.
            Main = new WiserItemModel();
            Lines = new List<WiserItemModel>();
        }

        public async Task ClearContentsAsync()
        {
            var id = Main.Id;
            Main = new WiserItemModel();
            Lines = new List<WiserItemModel>();
            Main.Id = id;

            // Delete all basket lines, so that we won't have floating basket lines in the database that aren't linked to any basket.
            await shoppingBasketsService.DeleteLinesAsync(id);
            await shoppingBasketsService.SaveAsync(Main, Lines, Settings);
        }

        #endregion

        #region Rendering

        private async Task<string> ReplaceTemplateAsync(string template)
        {
            if (String.IsNullOrWhiteSpace(template))
            {
                return String.Empty;
            }

            var output = template;

            var replacementData = new Dictionary<string, object>();
            if (output.Contains("{shippingcosts}"))
            {
                replacementData["shippingcosts"] = await shoppingBasketsService.CalculateShippingCostsAsync(Main, Lines, Settings);
            }
            if (output.Contains("{paymentmethodcosts}"))
            {
                replacementData["paymentmethodcosts"] = await shoppingBasketsService.CalculatePaymentMethodCostsAsync(Main, Lines, Settings);
            }

            output = StringReplacementsService.DoReplacements(output, replacementData);

            return output;
        }

        private async Task<string> GetRenderedBasketAsync(IDictionary<string, object> extraData = null)
        {
            var result = new StringBuilder();

            if (Lines?.Count == 0)
            {
                var template = Settings.TemplateEmpty ?? "";

                if (!String.IsNullOrWhiteSpace(template))
                {
                    if (!template.Contains($"id=\"GclShoppingBasketContainer{ComponentId}\"") && !String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
                    {
                        template = $"<div id=\"GclShoppingBasketContainer{ComponentId}\">{template}</div>";
                    }

                    template = StringReplacementsService.DoReplacements(template, Main.GetSortedList(true));
                }

                var header = Settings.AlwaysRenderHeaderAndFooter ? await ReplaceTemplateAsync(Settings.Header) : "";
                var footer = Settings.AlwaysRenderHeaderAndFooter ? await ReplaceTemplateAsync(Settings.Footer) : "";

                result.Append(header).Append(template).Append(footer);
            }
            else
            {
                var template = Settings.Template ?? "";
                template = await TemplatesService.DoReplacesAsync(DoDefaultShoppingBasketHtmlReplacements(template), false, false, false);

                var additionalReplacementData = new Dictionary<string, object>
                {
                    { "BasketLineStockActionMessage", basketLineStockActionMessage },
                    { "BasketLineValidityMessage", basketLineValidityMessage }
                };

                // Replace extra data first.
                if (extraData is { Count: > 0 })
                {
                    // The "couponExcludedItems" entry is a special case.
                    if (extraData.ContainsKey("couponExcludedItems") && extraData["couponExcludedItems"] is JArray { Count: > 0 } couponExcludedItemsArray)
                    {
                        StringReplacementsService.FillStringByClassList(couponExcludedItemsArray, template, true, "couponExcludedItemsRepeat");
                    }

                    extraData.Where(kvp => !kvp.Key.Equals("couponExcludedItems")).ToList().ForEach(kvp => additionalReplacementData[kvp.Key] = kvp.Value);
                }

                template = await shoppingBasketsService.ReplaceBasketInTemplateAsync(Main, Lines, Settings, template, stripNotExistingVariables: false, additionalReplacementData: additionalReplacementData);

                if (!template.Contains($"id=\"GclShoppingBasketContainer{ComponentId}\"") && !String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
                {
                    template = $"<div id=\"GclShoppingBasketContainer{ComponentId}\">{template}</div>";
                }

                var header = Settings.AlwaysRenderHeaderAndFooter ? await ReplaceTemplateAsync(Settings.Header) : "";
                var footer = Settings.AlwaysRenderHeaderAndFooter ? await ReplaceTemplateAsync(Settings.Footer) : "";

                result.Append(header).Append(template).Append(footer);
            }

            return await TemplatesService.DoReplacesAsync(DoDefaultShoppingBasketHtmlReplacements(result.ToString()), handleRequest: Settings.HandleRequest, evaluateLogicSnippets: Settings.EvaluateIfElseInTemplates, removeUnknownVariables: Settings.RemoveUnknownVariables);
        }

        /// <inheritdoc />
        public override async Task<HtmlString> InvokeAsync(DynamicContent dynamicContent, string callMethod, int? forcedComponentMode, Dictionary<string, string> extraData)
        {
            ComponentId = dynamicContent.Id;
            ExtraDataForReplacements = extraData;
            if (dynamicContent.Name is "JuiceControlLibrary.ShoppingBasket")
            {
                // Force component mode to Legacy mode if it was created through the JCL.
                Settings.ComponentMode = ComponentModes.Legacy;
            }

            ParseSettingsJson(dynamicContent.SettingsJson, forcedComponentMode);
            if (forcedComponentMode.HasValue)
            {
                Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
            }
            else if (!String.IsNullOrWhiteSpace(dynamicContent.ComponentMode))
            {
                Settings.ComponentMode = Enum.Parse<ComponentModes>(dynamicContent.ComponentMode);
            }

            HandleDefaultSettingsFromComponentMode();

            var (renderHtml, debugInformation) = await ShouldRenderHtmlAsync();
            if (!renderHtml)
            {
                return new HtmlString(debugInformation);
            }

            // Load the current basket.
            var (shoppingBasket, basketLines, validityMessage, stockActionMessage) = await shoppingBasketsService.LoadAsync(Settings);
            Main = shoppingBasket;
            Lines = basketLines;
            basketLineValidityMessage = validityMessage;
            basketLineStockActionMessage = stockActionMessage;

            var resultHtml = new StringBuilder();
            switch (Settings.ComponentMode)
            {
                case ComponentModes.Render:
                    resultHtml.Append(await HandleRenderModeAsync());
                    break;
                case ComponentModes.AddItems:
                    resultHtml.Append(await HandleAddItemsModeAsync());
                    break;
                case ComponentModes.UpdateItem:
                    resultHtml.Append(await HandleUpdateItemModeAsync());
                    break;
                case ComponentModes.ChangeQuantity:
                    resultHtml.Append(await HandleChangeQuantityModeAsync());
                    break;
                case ComponentModes.RemoveItems:
                    resultHtml.Append(await HandleRemoveItemsModeAsync());
                    break;
                case ComponentModes.AddCoupon:
                    resultHtml.Append(await HandleAddCouponModeAsync());
                    break;
                case ComponentModes.ClearContents:
                    resultHtml.Append(await HandleClearContentsModeAsync());
                    break;
                case ComponentModes.Reset:
                    resultHtml.Append(await HandleResetModeAsync());
                    break;
                case ComponentModes.Print:
                    resultHtml.Append(await HandlePrintModeAsync());
                    break;
                case ComponentModes.GeneratePdf:
                    resultHtml.Append(await HandleGeneratePdfModeAsync());
                    break;
                case ComponentModes.Email:
                    resultHtml.Append(await HandleEmailModeAsync());
                    break;
                case ComponentModes.Legacy:
                    resultHtml.Append(await HandleLegacyModeAsync());
                    break;
                default:
                    throw new NotImplementedException($"Unknown or unsupported component mode '{Settings.ComponentMode}' in 'InvokeAsync'.");
            }

            if (String.IsNullOrWhiteSpace(Settings.TemplateJavaScript))
            {
                return new HtmlString(resultHtml.ToString());
            }

            var javaScript = DoDefaultShoppingBasketHtmlReplacements(Settings.TemplateJavaScript);
            resultHtml.Append($"<script>{javaScript}</script>");

            return new HtmlString(resultHtml.ToString());
        }

        #endregion

        #region Handling different component modes

        public async Task<string> HandleRenderModeAsync()
        {
            return await GetRenderedBasketAsync();
        }

        /// <summary>
        /// Adds a new item to the basket.
        /// </summary>
        /// <param name="renderBasket">Whether the template should be rendered after adding is done.</param>
        /// <returns>The rendered template, or an empty string if <paramref name="renderBasket"/> is <see langword="false"/>.</returns>
        public async Task<string> HandleAddItemsModeAsync(bool renderBasket = true)
        {
            if (!String.IsNullOrWhiteSpace(HttpContextHelpers.GetRequestValue(HttpContext, "additem")))
            {
                await AddSingleItemAsync();
            }
            else
            {
                if (Request.Method.Equals("POST"))
                {
                    await AddMultipleItemsAsync();
                }
            }

            return renderBasket ? await GetRenderedBasketAsync() : String.Empty;
        }

        /// <summary>
        /// Updates an existing item in the basket.
        /// </summary>
        /// <param name="renderBasket">Whether the template should be rendered after updating is done.</param>
        /// <returns>The rendered template, or an empty string if <paramref name="renderBasket"/> is <see langword="false"/>.</returns>
        public async Task<string> HandleUpdateItemModeAsync(bool renderBasket = true)
        {
            using var reader = new StreamReader(Request.Body);
            var itemJson = await reader.ReadToEndAsync();

            // Convert the body to an UpdateItemModel object.
            var item = Newtonsoft.Json.JsonConvert.DeserializeObject<UpdateItemModel>(itemJson);
            if (item == null)
            {
                return String.Empty;
            }

            // Check if this line belongs to the current basket.
            if (Lines.All(l => l.Id != item.LineId))
            {
                Logger.LogWarning("Could not update line with ID '{lineId}'", item.LineId);
                return String.Empty;
            }

            // Update the line.
            await shoppingBasketsService.UpdateLineAsync(Main, Lines, Settings, item);

            return renderBasket ? await GetRenderedBasketAsync() : String.Empty;
        }

        private async Task AddSingleItemAsync()
        {
            var uniqueId = HttpContextHelpers.GetRequestValue(HttpContext, "uniqueid");
            if (!UInt64.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "additem"), out var itemId))
            {
                // No sense trying to add nothing.
                return;
            }

            if (!Int32.TryParse(HttpContextHelpers.GetRequestValue(HttpContext, "quantity"), NumberStyles.Float, CultureInfo.InvariantCulture, out var quantity))
            {
                quantity = 1;
            }

            var type = HttpContextHelpers.GetRequestValue(HttpContext, "type");
            if (String.IsNullOrWhiteSpace(type))
            {
                type = "product";
            }

            await shoppingBasketsService.AddLineAsync(Main, Lines, Settings, uniqueId, itemId, quantity, type);
        }

        private async Task AddMultipleItemsAsync()
        {
            using var reader = new StreamReader(Request.Body);
            var itemsJson = await reader.ReadToEndAsync();

            var lines = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<AddToShoppingBasketModel>>(itemsJson);
            await shoppingBasketsService.AddLinesAsync(Main, Lines, Settings, lines);
        }

        /// <summary>
        /// Retrieves the print template HTML.
        /// </summary>
        /// <returns></returns>
        private async Task<(string Html, ulong ContentItemId, string PdfDocumentOptions)> GetDocumentTemplateHtmlAsync(string overrideTemplate = null)
        {
            var html = "";
            var contentItemId = 0UL;
            var pdfDocumentOptions = "";
            var template = String.IsNullOrWhiteSpace(overrideTemplate) ? Settings.TemplatePrint : overrideTemplate;

            if (UInt64.TryParse(template, out var templateId))
            {
                contentItemId = templateId;

                var pdfDocumentOptionsPropertyName = await objectsService.FindSystemObjectByDomainNameAsync("pdf_documentoptionspropertyname", "documentoptions");
                var contentPropertyName = await objectsService.FindSystemObjectByDomainNameAsync("content_propertyname", "html_template");

                DatabaseConnection.ClearParameters();
                DatabaseConnection.AddParameter("id", templateId);
                DatabaseConnection.AddParameter("contentPropertyName", contentPropertyName);
                DatabaseConnection.AddParameter("pdfDocumentOptionsPropertyName", pdfDocumentOptionsPropertyName);
                var getTemplateResult = await DatabaseConnection.GetAsync($"SELECT `key`, CONCAT_WS('', `value`, long_value) AS `value` FROM {WiserTableNames.WiserItemDetail} WHERE item_id = ?id AND `key` IN (?contentPropertyName, ?pdfDocumentOptionsPropertyName)");
                if (getTemplateResult.Rows.Count > 0)
                {
                    foreach (DataRow dataRow in getTemplateResult.Rows)
                    {
                        var key = dataRow.Field<string>("key");
                        if (key == contentPropertyName)
                        {
                            html = dataRow.Field<string>("value");
                        }
                        else if (key == pdfDocumentOptionsPropertyName)
                        {
                            pdfDocumentOptions = dataRow.Field<string>("value");
                        }
                    }
                }
            }
            else
            {
                html = template;
            }

            // Add messages that were set while loading.
            var additionalReplacementData = new Dictionary<string, object>
            {
                { "BasketLineStockActionMessage", basketLineStockActionMessage },
                { "BasketLineValidityMessage", basketLineValidityMessage }
            };

            html = await TemplatesService.DoReplacesAsync(html, false, false, false);
            html = await shoppingBasketsService.ReplaceBasketInTemplateAsync(Main, Lines, Settings, html, stripNotExistingVariables: false, additionalReplacementData: additionalReplacementData);
            html = await StringReplacementsService.DoAllReplacementsAsync(html, null, Settings.HandleRequest, Settings.EvaluateIfElseInTemplates, Settings.RemoveUnknownVariables);
            html = StringReplacementsService.EvaluateTemplate(html);

            return (Settings.RemoveUnknownVariables ? Regex.Replace(html, "{[^\\]}\\s]*}", "") : html, contentItemId, pdfDocumentOptions);
        }

        public async Task<FileContentResult> GeneratePdfAsync()
        {
            var (html, contentItemId, pdfDocumentOptions) = await GetDocumentTemplateHtmlAsync();
            var pdfBackgroundPropertyName = await objectsService.FindSystemObjectByDomainNameAsync("pdf_backgroundpropertyname");
            var pdfFileResult = await htmlToPdfConverterService.ConvertHtmlStringToPdfAsync(new HtmlToPdfRequestModel { Html = html, ItemId = contentItemId, BackgroundPropertyName = pdfBackgroundPropertyName, DocumentOptions = pdfDocumentOptions });

            return pdfFileResult;
        }

        public async Task<string> HandleChangeQuantityModeAsync(bool renderBasket = true)
        {
            using var reader = new StreamReader(Request.Body);
            var updateQuantitiesJson = await reader.ReadToEndAsync();

            var items = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<UpdateQuantityModel>>(updateQuantitiesJson);
            if (items == null)
            {
                return renderBasket ? await GetRenderedBasketAsync() : String.Empty;
            }

            foreach (var item in items)
            {
                await shoppingBasketsService.UpdateBasketLineQuantityAsync(Main, Lines, Settings, item.Id, item.Quantity);
            }

            return renderBasket ? await GetRenderedBasketAsync() : String.Empty;
        }

        /// <summary>
        /// Attempts to remove an item from the basket and returns the rendered template.
        /// </summary>
        /// <param name="renderBasket">Whether the component should render the template.</param>
        /// <returns></returns>
        public async Task<string> HandleRemoveItemsModeAsync(bool renderBasket = true)
        {
            var itemId = HttpContextHelpers.GetRequestValue(HttpContext, "deleteproductbyid");
            if (!String.IsNullOrWhiteSpace(itemId))
            {
                await shoppingBasketsService.RemoveLinesAsync(Main, Lines, Settings, new[] { itemId });
            }
            else if (Request.HasFormContentType || Request.Method.InList("POST", "DELETE"))
            {
                using var reader = new StreamReader(Request.Body);
                var requestJson = await reader.ReadToEndAsync();

                var itemIds = Newtonsoft.Json.JsonConvert.DeserializeObject<IList<string>>(requestJson);
                await shoppingBasketsService.RemoveLinesAsync(Main, Lines, Settings, itemIds);
            }

            return renderBasket ? await GetRenderedBasketAsync() : String.Empty;
        }

        /// <summary>
        /// Attempts to add a coupon to the shopping basket and returns the rendered basket.
        /// </summary>
        /// <param name="renderBasket">Whether the component should render the template.</param>
        /// <returns></returns>
        public async Task<string> HandleAddCouponModeAsync(bool renderBasket = true)
        {
            var addCouponResult = await shoppingBasketsService.AddCouponToBasketAsync(Main, Lines, Settings);
            var replacementData = new Dictionary<string, object>
            {
                { "couponSuccess", addCouponResult.Valid ? "1" : "0" },
                { "addCouponResult", addCouponResult.ResultCode.ToString("G") }
            };

            // Add the excluded items as a JArray.
            var excludedItems = new JArray();
            if (addCouponResult.ExcludedItems is { Count: > 0 })
            {
                foreach (var excludedItem in addCouponResult.ExcludedItems)
                {
                    var obj = new JObject
                    {
                        { "ItemId", excludedItem.ItemId },
                        { "Name", excludedItem.Name }
                    };

                    excludedItems.Add(obj);
                }
            }

            replacementData.Add("couponExcludedItems", excludedItems);

            var result = renderBasket ? await GetRenderedBasketAsync(replacementData) : String.Empty;

            return result;
        }

        /// <summary>
        /// Clears the contents of the basket and returns the rendered template.
        /// </summary>
        /// <returns></returns>
        public async Task<string> HandleClearContentsModeAsync()
        {
            await ClearContentsAsync();

            return await GetRenderedBasketAsync();
        }

        /// <summary>
        /// Resets the basket and returns the rendered template.
        /// </summary>
        /// <returns></returns>
        public async Task<string> HandleResetModeAsync()
        {
            await ResetAsync();
            return await GetRenderedBasketAsync();
        }

        public async Task<string> HandlePrintModeAsync()
        {
            return (await GetDocumentTemplateHtmlAsync()).Html;
        }

        public async Task<string> HandleGeneratePdfModeAsync(string filename = null, bool saveToDisk = false)
        {
            var pdfFileResult = await GeneratePdfAsync();

            if (saveToDisk)
            {
                FileSystemHelpers.SaveFileToContentFilesFolder(webHostEnvironment, filename, pdfFileResult.FileContents);
            }
            else
            {
                pdfFileResult.FileDownloadName = !String.IsNullOrWhiteSpace(filename) ? Path.GetFileName(filename) : $"{Settings.Description}.pdf";
                await pdfFileResult.ExecuteResultAsync(HttpContextHelpers.CreateActionContext(HttpContext));
            }

            return String.Empty;
        }

        public async Task<string> HandleEmailModeAsync()
        {
            var htmlBody = (await GetDocumentTemplateHtmlAsync(Settings.EmailBody)).Html;

            var attachments = new List<ulong>();
            if (Settings.AddBasketAsPdfAttachment)
            {
                var filename = Path.GetFileName($"{Settings.Description}.pdf");
                var pdfFileResult = await GeneratePdfAsync();

                DatabaseConnection.ClearParameters();
                DatabaseConnection.AddParameter("content_type", "application/pdf");
                DatabaseConnection.AddParameter("content", pdfFileResult.FileContents);
                DatabaseConnection.AddParameter("file_name", filename);
                DatabaseConnection.AddParameter("extension", Path.GetExtension(filename));
                DatabaseConnection.AddParameter("added_by", "GCL");
                attachments.Add(await DatabaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(WiserTableNames.WiserItemFile, 0UL));
            }

            var emailAddress = HttpContextHelpers.GetRequestValue(HttpContext, "emailaddress");

            await communicationsService.SendEmailAsync(emailAddress, Settings.EmailSubject, htmlBody, attachments: attachments);

            return await GetRenderedBasketAsync();
        }

        /// <summary>
        /// Handles the Legacy component mode, which basically mimics the JCL ShoppingBasket.
        /// </summary>
        /// <returns></returns>
        public async Task<string> HandleLegacyModeAsync()
        {
            // Check mode request variable.
            var mode = HttpContextHelpers.GetRequestValue(HttpContext, "mode");
            if (!String.IsNullOrWhiteSpace(mode))
            {
                switch (mode)
                {
                    case "printbasket":
                        return await HandlePrintModeAsync();
                    case "generatepdf":
                        return await HandleGeneratePdfModeAsync();
                    case "emailbasket":
                        return await HandleEmailModeAsync();
                }
            }

            // Check other request variables ("additem", "deleteproductbyid", etc.).
            if (!String.IsNullOrWhiteSpace(HttpContextHelpers.GetRequestValue(HttpContext, "additem")) || !String.IsNullOrWhiteSpace(HttpContextHelpers.GetRequestValue(HttpContext, "additems")))
            {
                await HandleAddItemsModeAsync();
            }

            var removeItemRequest = HttpContextHelpers.GetRequestValue(HttpContext, "deleteproductbyid");
            if (!String.IsNullOrWhiteSpace(removeItemRequest))
            {
                await HandleRemoveItemsModeAsync();
            }

            if (Settings.HandleRequest)
            {
                // Check if the quantity of an item (or quantities of multiple items) should be changed.
                if (mode == "aantallenaangepast")
                {
                    foreach (var key in Request.Query.Keys.Where(key => key.StartsWith("aantal_", StringComparison.OrdinalIgnoreCase)))
                    {
                        var id = key[7..];
                        var quantity = Convert.ToInt32(Request.Query[key].First());
                        await shoppingBasketsService.UpdateBasketLineQuantityAsync(Main, Lines, Settings, id, quantity);
                    }

                    if (Request.HasFormContentType)
                    {
                        foreach (var key in Request.Form.Keys.Where(key => key.StartsWith("aantal_", StringComparison.OrdinalIgnoreCase)))
                        {
                            var id = key[7..];
                            var quantity = Convert.ToInt32(Request.Form[key].First());
                            await shoppingBasketsService.UpdateBasketLineQuantityAsync(Main, Lines, Settings, id, quantity);
                        }
                    }
                }

                // Handle potential coupon.
                await shoppingBasketsService.AddCouponToBasketAsync(Main, Lines, Settings);
            }

            if (Settings.ClearContentsOnLoad)
            {
                // Clear the contents (does NOT create a new basket).
                await ClearContentsAsync();
            }

            if (Settings.ResetOnLoad)
            {
                // Reset the basket (creates a new basket).
                await ResetAsync();
            }

            await shoppingBasketsService.RecalculateVariablesAsync(Main, Lines, Settings);

            return await GetRenderedBasketAsync();
        }

        private string DoDefaultShoppingBasketHtmlReplacements(string template)
        {
            return template.ReplaceCaseInsensitive("{contentId}", ComponentId.ToString()).ReplaceCaseInsensitive("{basketId}", Main.Id.ToString());
        }

        #endregion

        #region Handling settings

        /// <inheritdoc />
        public override void ParseSettingsJson(string settingsJson, int? forcedComponentMode = null)
        {
            if (String.IsNullOrWhiteSpace(settingsJson))
            {
                return;
            }

            if (Settings.ComponentMode == ComponentModes.Legacy)
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ShoppingBasketLegacySettingsModel>(settingsJson)?.ToSettingsModel();
                // Parsing the settings will set the component mode to Render, so force it back to Legacy here.
                if (Settings != null)
                {
                    Settings.ComponentMode = ComponentModes.Legacy;
                }
            }
            else
            {
                Settings = Newtonsoft.Json.JsonConvert.DeserializeObject<ShoppingBasketCmsSettingsModel>(settingsJson);
                if (forcedComponentMode.HasValue && Settings != null)
                {
                    Settings.ComponentMode = (ComponentModes)forcedComponentMode.Value;
                }
            }
        }

        /// <inheritdoc />
        public override string GetSettingsJson()
        {
            return Settings.ComponentMode == ComponentModes.Legacy
                ? Newtonsoft.Json.JsonConvert.SerializeObject(new ShoppingBasketLegacySettingsModel().FromSettingsModel(Settings))
                : Newtonsoft.Json.JsonConvert.SerializeObject(Settings);
        }

        #endregion
    }
}
