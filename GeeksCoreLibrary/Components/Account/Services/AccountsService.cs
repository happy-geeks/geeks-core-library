﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Interfaces;
using GeeksCoreLibrary.Components.Account.Models;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Extensions;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Interfaces;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace GeeksCoreLibrary.Components.Account.Services
{
    public class AccountsService : IAccountsService, IScopedService
    {
        private readonly GclSettings gclSettings;
        private readonly IDatabaseConnection databaseConnection;
        private readonly IHttpContextAccessor httpContextAccessor;
        private readonly IObjectsService objectsService;
        private readonly ILogger<AccountsService> logger;
        private readonly IDatabaseHelpersService databaseHelpersService;
        private readonly IRolesService rolesService;
        private readonly IServiceProvider serviceProvider;
        private readonly IReplacementsMediator replacementsMediator;

        public AccountsService(IOptions<GclSettings> gclSettings,
                               IDatabaseConnection databaseConnection,
                               IObjectsService objectsService,
                               ILogger<AccountsService> logger,
                               IDatabaseHelpersService databaseHelpersService,
                               IRolesService rolesService,
                               IServiceProvider serviceProvider,
                               IReplacementsMediator replacementsMediator,
                               IHttpContextAccessor httpContextAccessor = null)
        {
            this.gclSettings = gclSettings.Value;
            this.databaseConnection = databaseConnection;
            this.httpContextAccessor = httpContextAccessor;
            this.objectsService = objectsService;
            this.logger = logger;
            this.databaseHelpersService = databaseHelpersService;
            this.rolesService = rolesService;
            this.serviceProvider = serviceProvider;
            this.replacementsMediator = replacementsMediator;
        }

        /// <inheritdoc />
        public async Task<UserCookieDataModel> GetUserDataFromCookieAsync(string cookieName = Constants.CookieName)
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext is null.");
            }

            // Check if we already have the user data cached this life cycle and return it from the cache if that is the case.
            if (httpContext.Items.ContainsKey(Constants.UserDataCachingKey + cookieName))
            {
                return (UserCookieDataModel) httpContext.Items[Constants.UserDataCachingKey + cookieName];
            }

            var defaultAnonymousUserModel = new UserCookieDataModel();

            try
            {
                // Get the ID of the default user to use for anonymous users (users who are not logged in).
                // This can be used to set permissions for items, so that users who are not logged in cannot get/update/create/delete certain items.
                var defaultAnonymousUserIdValue = await objectsService.FindSystemObjectByDomainNameAsync("defaultAnonymousUserId", "0");
                UInt64.TryParse(defaultAnonymousUserIdValue, out var defaultAnonymousUserId);
                defaultAnonymousUserModel.MainUserId = defaultAnonymousUserId;
                defaultAnonymousUserModel.UserId = defaultAnonymousUserId;

                var cookieValue = httpContext.Request.Cookies[cookieName];
                if (String.IsNullOrWhiteSpace(cookieValue))
                {
                    return defaultAnonymousUserModel;
                }

                var cookieValueParts = cookieValue.Split(':');
                if (cookieValueParts.Length != 3)
                {
                    // Delete the cookie, it's not valid (anymore).
                    httpContext.Response.Cookies.Delete(cookieName);
                    return defaultAnonymousUserModel;
                }

                await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { Constants.AuthenticationTokensTableName });

                // Note: Added the word 'update' to the query force the GCL to use the write connection string.
                // Note: This is done because sometimes the sync to a read database is not instant and then the cookie cannot be found immediately after creating it.
                var query = $@"# UPDATE
                                SELECT 
                                    user_id, 
                                    main_user_id,
                                    hashed_validator,
                                    login_date,
                                    expires,
                                    ip_address,
                                    user_agent,
                                    role,
                                    main_user_entity_type
                                FROM {Constants.AuthenticationTokensTableName}
                                WHERE selector = ?selector
                                AND entity_type = ?entityType
                                AND expires > NOW()";

                databaseConnection.AddParameter("selector", cookieValueParts[0]);
                databaseConnection.AddParameter("entityType", cookieValueParts[2]);
                var result = await databaseConnection.GetAsync(query, true);

                if (result.Rows.Count == 0)
                {
                    // Delete the cookie, it's not valid (anymore).
                    httpContext.Response.Cookies.Delete(cookieName);
                    return defaultAnonymousUserModel;
                }

                var dataSetFirstRow = result.Rows[0];
                var hashedValidator = dataSetFirstRow.Field<string>("hashed_validator");
                var userId = Convert.ToUInt64(dataSetFirstRow["user_id"]);
                var mainUserId = Convert.ToUInt64(dataSetFirstRow["main_user_id"]);
                if (!Uri.UnescapeDataString(cookieValueParts[1]).DecryptWithAes(gclSettings.AccountCookieValueEncryptionKey).VerifySha512(hashedValidator))
                {
                    // Delete the cookie, it's not valid (anymore).
                    httpContext.Response.Cookies.Delete(cookieName);
                    return defaultAnonymousUserModel;
                }

                var output = new UserCookieDataModel
                {
                    UserId = userId,
                    MainUserId = mainUserId,
                    Selector = new Guid(cookieValueParts[0]),
                    EntityType = cookieValueParts[2],
                    Expires = dataSetFirstRow.Field<DateTime>("expires"),
                    LoginDate = dataSetFirstRow.Field<DateTime>("login_date"),
                    IpAddress = dataSetFirstRow.Field<string>("ip_address"),
                    UserAgent = dataSetFirstRow.Field<string>("user_agent"),
                    MainUserEntityType = dataSetFirstRow.Field<string>("main_user_entity_type"),
                    Roles = await GetUserRolesAsync(userId),
                    ExtraData = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase)
                };

                var extraDataQuery = await objectsService.FindSystemObjectByDomainNameAsync("Account_ExtraDataQuery");
                if (String.IsNullOrWhiteSpace(extraDataQuery))
                {
                    extraDataQuery = await objectsService.FindSystemObjectByDomainNameAsync("AccountWiser2_ExtraDataQuery");
                }

                if (!String.IsNullOrWhiteSpace(extraDataQuery))
                {
                    try
                    {
                        result = await databaseConnection.GetAsync(extraDataQuery
                            .Replace("{Account_UserId}", output.UserId.ToString())
                            .Replace("{AccountWiser2_UserId}", output.UserId.ToString())
                            .Replace("{Account_MainUserId}", output.MainUserId.ToString())
                            .Replace("{AccountWiser2_MainUserId}", output.MainUserId.ToString()), true);

                        if (result.Rows.Count > 0)
                        {
                            var dataRow = result.Rows[0];
                            foreach (DataColumn dataColumn in result.Columns)
                            {
                                if (output.ExtraData.ContainsKey(dataColumn.ColumnName))
                                {
                                    continue;
                                }

                                output.ExtraData.Add(dataColumn.ColumnName, dataRow.IsNull(dataColumn.ColumnName) ? "" : dataRow[dataColumn.ColumnName].ToString());
                            }
                        }
                    }
                    catch (Exception exception)
                    {
                        logger.LogError($"An error occurred while getting extra data for logged in user: {exception}", true);
                    }
                }

                logger.LogTrace($"Gotten user information in Account and saved it to lifecycle cache: {Newtonsoft.Json.JsonConvert.SerializeObject(output)}");

                // Save to http context (caching during lifecycle).
                httpContext.Items.Add(Constants.UserDataCachingKey + cookieName, output);

                return output;
            }
            catch (Exception exception)
            {
                logger.LogError($"Account - Exception occurred in GetUserIdFromCookie: {exception}");
                return defaultAnonymousUserModel;
            }
        }

        /// <inheritdoc />
        public ulong GetRecentlyCreatedAccountId()
        {
            var httpContext = httpContextAccessor?.HttpContext;
            if (httpContext == null)
            {
                throw new Exception("HttpContext is null.");
            }

            var result = 0UL;
            try
            {
                var cookieValue = httpContext.Request.Cookies[Constants.CreatedAccountCookieName];
                if (String.IsNullOrWhiteSpace(cookieValue))
                {
                    return result;
                }

                _ = UInt64.TryParse(cookieValue.DecryptWithAes(gclSettings.AccountUserIdEncryptionKey), out result);
            }
            catch (Exception exception)
            {
                logger.LogError($"An error occurred while reading the cookie for a recently created account: {exception}");
            }

            return result;
        }

        /// <inheritdoc />
        public async Task<string> GenerateNewCookieTokenAsync(ulong userId, ulong mainUserId, int amountOfDaysToRememberCookie, string mainUserEntityType = "relatie", string userEntityType = "account")
        {
            // Make sure we always have a valid main user ID. If the user is logging in with a main user, this should be the same as the user ID.
            if (mainUserId == 0)
            {
                mainUserId = userId;
            }

            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { Constants.AuthenticationTokensTableName });

            // Delete all expired token, there is no point in keeping them.
            await databaseConnection.ExecuteAsync($"DELETE FROM {Constants.AuthenticationTokensTableName} WHERE expires <= NOW()");

            // Generate the new token and add it to the database.
            var validator = Guid.NewGuid().ToString("N");
            var selector = Guid.NewGuid().ToString("N");
            var entityTypeToUse = mainUserId != userId ? userEntityType : mainUserEntityType;
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("selector", selector);
            databaseConnection.AddParameter("hashed_validator", validator.ToSha512ForPasswords());
            databaseConnection.AddParameter("user_id", userId);
            databaseConnection.AddParameter("main_user_id", mainUserId);
            databaseConnection.AddParameter("entity_type", entityTypeToUse);
            databaseConnection.AddParameter("main_user_entity_type", mainUserEntityType);
            databaseConnection.AddParameter("ip_address", HttpContextHelpers.GetUserIpAddress(httpContextAccessor?.HttpContext));
            databaseConnection.AddParameter("user_agent", HttpContextHelpers.GetHeaderValueAs<string>(httpContextAccessor?.HttpContext, HeaderNames.UserAgent));
            databaseConnection.AddParameter("expires", DateTime.Now.AddDays(amountOfDaysToRememberCookie));
            databaseConnection.AddParameter("login_date", DateTime.Now);
            await databaseConnection.InsertOrUpdateRecordBasedOnParametersAsync(Constants.AuthenticationTokensTableName, 0UL);

            return $"{selector}:{Uri.EscapeDataString(validator.EncryptWithAes(gclSettings.AccountCookieValueEncryptionKey))}:{entityTypeToUse}";
        }

        /// <inheritdoc />
        public async Task RemoveCookieTokenAsync(string selector)
        {
            databaseConnection.AddParameter("selector", selector);
            await databaseHelpersService.CheckAndUpdateTablesAsync(new List<string> { Constants.AuthenticationTokensTableName });
            await databaseConnection.ExecuteAsync($"DELETE FROM {Constants.AuthenticationTokensTableName} WHERE selector = ?selector");
        }

        /// <inheritdoc />
        public async Task Save2FactorAuthenticationKeyAsync(ulong userId, string user2FactorAuthenticationKey, string entityType = Constants.DefaultEntityType)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var wiserItemsService = scope.ServiceProvider.GetRequiredService<IWiserItemsService>();
            await wiserItemsService.SaveItemDetailAsync(new WiserItemDetailModel
            {
                Key = Constants.TotpFieldName,
                Value = user2FactorAuthenticationKey
            }, userId, entityType: entityType);
        }

        /// <inheritdoc />
        public async Task<string> Get2FactorAuthenticationKeyAsync(ulong userId, string entityType = Constants.DefaultEntityType)
        {
            await using var scope = serviceProvider.CreateAsyncScope();
            var wiserItemsService = scope.ServiceProvider.GetRequiredService<IWiserItemsService>();
            var user = await wiserItemsService.GetItemDetailsAsync(userId, entityType: entityType, skipPermissionsCheck: true);
            return user.GetDetailValue(Constants.TotpFieldName);
        }

        /// <inheritdoc />
        public async Task LogoutUserAsync(AccountCmsSettingsModel settings, bool isAutoLogout = false)
        {
            // Do some initial checks, to make sure we have everything we need and the user is actually still logged in.
            var currentContext = httpContextAccessor?.HttpContext;
            if (currentContext == null)
            {
                logger.LogError("HttpContext is null, can't log out the user!");
                return;
            }

            var cookieValue = currentContext.Request.Cookies[settings.CookieName];
            if (String.IsNullOrWhiteSpace(cookieValue))
            {
                return;
            }

            var cookieValueParts = cookieValue.Split(':');
            if (cookieValueParts.Length != 3)
            {
                logger.LogWarning($"User has an invalid cookie: '{cookieValue}'");
                return;
            }

            var ociUrl = currentContext.Request.Cookies[Constants.OciHookUrlCookieName];

            // Delete the cookie(s).
            currentContext.Response.Cookies.Delete(settings.CookieName);
            if (!String.IsNullOrWhiteSpace(ociUrl))
            {
                currentContext.Response.Cookies.Delete(Constants.OciHookUrlCookieName);
            }

            var cookiesToDelete = (settings.CookiesToDeleteAfterLogout ?? "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries).ToList();

            // Get basket cookie name from settings and delete that cookie.
            var basketCookieName = await objectsService.FindSystemObjectByDomainNameAsync("BASKET_cookieName");
            if (!String.IsNullOrWhiteSpace(basketCookieName) && !cookiesToDelete.Contains(basketCookieName))
            {
                cookiesToDelete.Add(basketCookieName);
            }

            // Also delete any cookies with the default cookie name constant.
            if (!cookiesToDelete.Contains(ShoppingBasket.Models.Constants.DefaultCookieName))
            {
                cookiesToDelete.Add(ShoppingBasket.Models.Constants.DefaultCookieName);
            }

            foreach (var cookieToDelete in cookiesToDelete)
            {
                currentContext.Response.Cookies.Delete(cookieToDelete);
            }

            // Remove session values.
            var punchOutSessionPrefix = await objectsService.FindSystemObjectByDomainNameAsync("CXmlPunchOutSessionPrefix");
            currentContext.Session.Remove($"{punchOutSessionPrefix}session_token");
            currentContext.Session.Remove($"{punchOutSessionPrefix}organisatie_id");
            currentContext.Session.Remove($"{punchOutSessionPrefix}user_id");
            currentContext.Session.Remove($"{punchOutSessionPrefix}hook_url");
            currentContext.Session.Remove($"{punchOutSessionPrefix}buyer_cookie");
            currentContext.Session.Remove($"{punchOutSessionPrefix}duns_from");
            currentContext.Session.Remove($"{punchOutSessionPrefix}duns_to");
            currentContext.Session.Remove($"{punchOutSessionPrefix}duns_sender");

            // Remove any session extra values from the setting SessionKeysToDeleteAfterLogout.
            var sessionsToDelete = (settings.SessionKeysToDeleteAfterLogout ?? "").Split(new[] { '.' }, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            foreach (var sessionToDelete in sessionsToDelete)
            {
                currentContext.Session.Remove(sessionToDelete);
            }

            // Remove any session values that might have been added via Wiser login.
            var extraSessionKeysToRemove = currentContext.Session.Keys.Where(s => s.StartsWith("WiserLogin_", StringComparison.OrdinalIgnoreCase));
            foreach (var sessionToDelete in extraSessionKeysToRemove)
            {
                currentContext.Session.Remove(sessionToDelete);
            }

            await RemoveCookieTokenAsync(cookieValueParts[0]);

            if (!String.IsNullOrWhiteSpace(ociUrl))
            {
                currentContext.Response.Headers.Add($"x-{settings.OciHookUrlKey}", ociUrl);

                if (settings.EnableOciLogin && !isAutoLogout)
                {
                    currentContext.Response.Redirect(ociUrl);
                }
            }
        }

        /// <inheritdoc />
        public async Task<string> DoAccountReplacementsAsync(string input, bool forQuery = false)
        {
            if (!input.Contains("{AccountWiser2_", StringComparison.OrdinalIgnoreCase) && !input.Contains("{Account_"))
            {
                return input;
            }

            var userData = await GetUserDataFromCookieAsync();

            var replaceData = new Dictionary<string, object>(StringComparer.OrdinalIgnoreCase);
            foreach (PropertyDescriptor property in TypeDescriptor.GetProperties(userData))
            {
                replaceData.Add(property.Name, property.GetValue(userData) ?? "");
            }

            if (userData.ExtraData != null)
            {
                foreach (var keyValuePair in userData.ExtraData)
                {
                    if (replaceData.ContainsKey(keyValuePair.Key))
                    {
                        continue;
                    }

                    replaceData.Add(keyValuePair.Key, keyValuePair.Value);
                }
            }

            input = replacementsMediator.DoReplacements(input, replaceData, forQuery: forQuery, prefix: "{Account_");
            input = replacementsMediator.DoReplacements(input, replaceData, forQuery: forQuery, prefix: "{AccountWiser2_");

            return input;
        }

        /// <inheritdoc />
        public async Task<List<RoleModel>> GetUserRolesAsync(ulong userId, bool includePermissions = false)
        {
            databaseConnection.AddParameter("userId", userId);
            var rolesData = await databaseConnection.GetAsync($"SELECT role_id FROM `{WiserTableNames.WiserUserRoles}` WHERE user_id = ?userId");
            if (rolesData.Rows.Count == 0)
            {
                return new List<RoleModel>(0);
            }

            // Turn the retrieved role IDs into a List of integers.
            var userRoleIds = rolesData.Rows.Cast<DataRow>().Select(dataRow => Convert.ToInt32(dataRow["role_id"])).ToList();

            // Retrieve all rows.
            var roles = await rolesService.GetRolesAsync(includePermissions);

            // Filter the roles based on the user's role IDs and return a List of the remaining rows.
            return roles.Where(role => userRoleIds.Contains(role.Id)).ToList();
        }
    }
}