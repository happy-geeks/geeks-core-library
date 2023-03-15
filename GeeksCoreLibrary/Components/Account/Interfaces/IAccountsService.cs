using System;
﻿using System.Collections.Generic;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Core.Models;

namespace GeeksCoreLibrary.Components.Account.Interfaces
{
    public interface IAccountsService
    {
        /// <summary>
        /// This function gets the user ID from a cookie of the user.
        /// If the user has no cookie, the cookie contains an invalid value or the token is expired, it will return 0.
        /// </summary>
        /// <returns>If the user has no cookie, the cookie contains an invalid value or the token is expired, it will return 0, otherwise the ID of the user.</returns>
        Task<UserCookieDataModel> GetUserDataFromCookieAsync();

        /// <summary>
        /// Sometimes users want to place an order without creating an account. We do not want to login these users, but we do need an 'account' to link the order too.
        /// So we still create an account for the user, but without a password. We then save this ID encrypted in a cookie to be used later. This function gets and decrypts that ID.
        /// </summary>
        /// <returns>The decrypted user ID if a user was recently created in this session. Otherwise it returns 0.</returns>
        ulong GetRecentlyCreateAccountId();

        /// <summary>
        /// Generates a new cookie for a logged in user.
        /// </summary>
        /// <param name="userId">The ID of the user.</param>
        /// <param name="mainUserId">The ID of the main user, if the user logged in with a sub account.</param>
        /// <param name="amountOfDaysToRememberCookie">The amount of days to remember the cookie for the user.</param>
        /// <param name="mainUserEntityType">The entity type for main accounts.</param>
        /// <param name="userEntityType">The entity type for sub accounts.</param>
        /// <returns>The value that should be saved in the cookie.</returns>
        Task<string> GenerateNewCookieTokenAsync(ulong userId, ulong mainUserId, int amountOfDaysToRememberCookie, string mainUserEntityType = "relatie", string userEntityType = "account");
        
        /// <summary>
        /// Deletes a cookie token from the database, so that the user cannot login with it anymore, even if it still has a cookie with that token.
        /// </summary>
        /// <param name="selector">The selector of the token.</param>
        Task RemoveCookieTokenAsync(string selector);

        /// <summary>
        /// Saves the 2FA key from the user in the database
        /// </summary>
        /// <param name="userId">The User ID of the user</param>
        /// <param name="user2FactorAuthenticationKey">A random generated string used for authentication</param>
        /// <returns></returns>
        Task Save2FactorAuthenticationKeyAsync(ulong userId, string user2FactorAuthenticationKey);

        /// <summary>
        /// Gets the 2FA key from the user in the database
        /// </summary>
        /// <param name="userId">The ID of the user</param>
        /// <returns></returns>
        Task<string> Get2FactorAuthenticationKeyAsync(ulong userId);
        
        /// <summary>
        /// Attempts to log off the user. This will delete the user's cookie from their browser and our database.
        /// </summary>
        /// <param name="settings">The settings of the account component.</param>
        /// <param name="isAutoLogout">Optional: Whether this is an automatic logout via code or not.</param>
        Task LogoutUserAsync(AccountCmsSettingsModel settings, bool isAutoLogout = false);

        /// <summary>
        /// Do all replacements for the account component on a string.
        /// </summary>
        /// <param name="input">The input string.</param>
        /// <param name="forQuery">Optional: Set to <see langword="true"/> to make all replaced values safe against SQL injection.</param>
        /// <returns>The result string.</returns>
        Task<string> DoAccountReplacementsAsync(string input, bool forQuery = false);

        /// <summary>
        /// Gets all roles a user has.
        /// </summary>
        /// <param name="userId">The item ID of the user whose roles to retrieve.</param>
        /// <param name="includePermissions">Optional: Whether to include all permissions that each role has. Default is <see langword="false"/>.</param>
        /// <returns></returns>
        Task<List<RoleModel>> GetUserRolesAsync(ulong userId, bool includePermissions = false);
    }
}
