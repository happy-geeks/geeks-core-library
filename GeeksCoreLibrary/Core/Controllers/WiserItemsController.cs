using System;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Core.Enums;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Languages.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace GeeksCoreLibrary.Core.Controllers
{
    [Area("WiserItems")]
    [Route("wiser-items")]
    public class WiserItemsController : Controller
    {
        private readonly IWiserItemsService wiserItemsService;
        private readonly IAccountsService accountsService;
        private readonly ILanguagesService languagesService;

        public WiserItemsController(IWiserItemsService wiserItemsService, IAccountsService accountsService, ILanguagesService languagesService)
        {
            this.wiserItemsService = wiserItemsService;
            this.accountsService = accountsService;
            this.languagesService = languagesService;
        }

        /// <summary>
        /// Gets an item from Wiser as an JSON object.
        /// This function will also check the rights of the item. If the user is not allowed to see this item, the function will return a HTTP 403 (Forbidden).
        /// </summary>
        /// <param name="id">The encrypted ID of the item.</param>
        /// <param name="entityType">Optional: The entity type of the item. This is required if the item is saved in a table other than "wiser_item".</param>
        /// <returns>The Wiser item as an JSON object.</returns>
        [HttpGet, Route("{id}")]
        public async Task<IActionResult> GetAsync(string id, string entityType = null)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return BadRequest("No ID given");
            }

            if (!StringHelpers.TryDecryptWithAesWithSalt(id, out var decryptedId, withDateTime: true) || !UInt64.TryParse(decryptedId, out var itemId) || itemId == 0) 
            {
                return BadRequest("Invalid ID given");
            }

            var userData = await accountsService.GetUserDataFromCookieAsync();
            var (isPossible, _, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Read, onlyCheckAccessRights: true, entityType: entityType, userId: userData.UserId);
            if (!isPossible)
            {
                return new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }

            var item = await wiserItemsService.GetItemDetailsAsync(itemId, languageCode: languagesService.CurrentLanguageCode, entityType: entityType);

            return new ObjectResult(item);
        }

        /// <summary>
        /// Creates a new wiser item in the database.
        /// This function will also check the rights of the item. If the user is not allowed to create this item, the function will return a HTTP 403 (Forbidden).
        /// </summary>
        /// <param name="item">The item to create.</param>
        /// <param name="parentItemId">Optional: The encrypted ID of the parent. The new item will then be linked to the parent.</param>
        /// <param name="linkType">Optional: The link type to use to link the item to it's parent. Default value is 1.</param>
        /// <returns></returns>
        [HttpPost]
        public async Task<IActionResult> CreateAsync([FromBody]WiserItemModel item, [FromQuery]string parentItemId = null, [FromQuery]int linkType = 1)
        {
            if (item == null)
            {
                return BadRequest("No data given");
            }

            ulong itemId = 0;
            if (!String.IsNullOrWhiteSpace(parentItemId) && (!StringHelpers.TryDecryptWithAesWithSalt(parentItemId, out var decryptedId, withDateTime: true) || !UInt64.TryParse(decryptedId, out itemId))) 
            {
                return BadRequest("Invalid parent item ID given");
            }

            var userData = await accountsService.GetUserDataFromCookieAsync();
            var (isPossible, _, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Create, entityType: item.EntityType, userId: userData.UserId);
            if (!isPossible)
            {
                return new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }

            // Make sure to set the ID to 0, so that we can't accidentally update an existing item.
            item.Id = 0;
            item.OriginalItemId = 0;
            var newItem = await wiserItemsService.SaveAsync(item, itemId, linkType, userData.UserId);

            return new ObjectResult(newItem);
        }

        /// <summary>
        /// Updates an existing wiser item in the database.
        /// This function will also check the rights of the item. If the user is not allowed to update this item, the function will return a HTTP 403 (Forbidden).
        /// </summary>
        /// <param name="id">The encrypted ID of the item.</param>
        /// <param name="item">The item to create.</param>
        /// <returns></returns>
        [HttpPut, Route("{id}")]
        public async Task<IActionResult> UpdateAsync(string id, [FromBody]WiserItemModel item)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return BadRequest("No ID given");
            }

            if (!StringHelpers.TryDecryptWithAesWithSalt(id, out var decryptedId, withDateTime: true) || !UInt64.TryParse(decryptedId, out var itemId) || itemId == 0) 
            {
                return BadRequest("Invalid ID given");
            }
            
            if (item == null)
            {
                return BadRequest("No data given");
            }

            var userData = await accountsService.GetUserDataFromCookieAsync();
            var (isPossible, _, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Update, entityType: item.EntityType, userId: userData.UserId);
            if (!isPossible)
            {
                return new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }
            
            await wiserItemsService.UpdateAsync(itemId, item, userData.UserId);

            return NoContent();
        }

        /// <summary>
        /// Deletes an existing wiser item in the database.
        /// This function will also check the rights of the item. If the user is not allowed to update this item, the function will return a HTTP 403 (Forbidden).
        /// </summary>
        /// <param name="id">The encrypted ID of the item.</param>
        /// <param name="entityType">Optional: The entity type of the item. This is required if the item is saved in a table other than "wiser_item".</param>
        /// <returns></returns>
        [HttpDelete, Route("{id}")]
        public async Task<IActionResult> DeleteAsync(string id, string entityType = null)
        {
            if (String.IsNullOrWhiteSpace(id))
            {
                return BadRequest("No ID given");
            }

            if (!StringHelpers.TryDecryptWithAesWithSalt(id, out var decryptedId, withDateTime: true) || !UInt64.TryParse(decryptedId, out var itemId) || itemId == 0) 
            {
                return BadRequest("Invalid ID given");
            }

            var userData = await accountsService.GetUserDataFromCookieAsync();
            var (isPossible, _, _) = await wiserItemsService.CheckIfEntityActionIsPossibleAsync(itemId, EntityActions.Delete, entityType: entityType, userId: userData.UserId);
            if (!isPossible)
            {
                return new ObjectResult("Forbidden") { StatusCode = StatusCodes.Status403Forbidden };
            }
            
            await wiserItemsService.DeleteAsync(itemId, userId: userData.UserId, entityType: entityType);

            return NoContent();
        }
    }
}
