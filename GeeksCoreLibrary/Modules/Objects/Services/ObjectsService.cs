using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Core.Models;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.Objects.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace GeeksCoreLibrary.Modules.Objects.Services
{
    public class ObjectsService : IObjectsService, IScopedService
    {
        private readonly IDatabaseConnection databaseConnection;
        private readonly GclSettings gclSettings;
        private readonly IHttpContextAccessor httpContextAccessor;

        /// <summary>
        /// Initializes a new instance of <see cref="ObjectsService"/>.
        /// </summary>
        public ObjectsService(IDatabaseConnection databaseConnection, IOptions<GclSettings> gclSettings, IHttpContextAccessor httpContextAccessor = null)
        {
            this.databaseConnection = databaseConnection;
            this.gclSettings = gclSettings.Value;
            this.httpContextAccessor = httpContextAccessor;
        }

        /// <inheritdoc />
        public async Task<string> FindSystemObjectByDomainNameAsync(string objectKey, string defaultResult = "", string overrideDomain = "", bool searchFromSpecificToGeneral = true, bool stripAllLowerLevelDomains = false, bool throwErrorIfEmpty = false)
        {
            string finalResult;
            var domain = overrideDomain;
            if (String.IsNullOrEmpty(domain))
            {
                domain = HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false);
            }

            if (searchFromSpecificToGeneral)
            {
                // By passing an empty list to the testDomains parameters, no test domains will be checked.
                finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, new List<string>(), true, includePort: false)}");

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includingTestWww: true, includePort: false)}");
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");

                    if (stripAllLowerLevelDomains && String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        while (domain.Contains(".") && String.IsNullOrEmpty(finalResult))
                        {
                            domain = domain[(domain.IndexOf(".", StringComparison.Ordinal) + 1)..];
                            finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");
                        }
                    }
                    else if (!String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false)}");
                    }
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false).Split('.')[0]}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_url_{HttpContextHelpers.GetUrlPrefix(httpContextAccessor?.HttpContext, gclSettings.IndexOfLanguagePartInUrl)}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync(objectKey);
                }
            }
            else
            {
                finalResult = await GetSystemObjectValueAsync(objectKey);
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_url_{HttpContextHelpers.GetUrlPrefix(httpContextAccessor?.HttpContext, gclSettings.IndexOfLanguagePartInUrl)}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false).Split('.')[0]}");
                }
                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");

                    if (stripAllLowerLevelDomains && String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        while (domain.Contains(".") && String.IsNullOrEmpty(finalResult))
                        {
                            domain = domain[(domain.IndexOf(".", StringComparison.Ordinal) + 1)..];
                            finalResult = await GetSystemObjectValueAsync($"{objectKey}_{domain}");
                        }
                    }
                    else if (!String.IsNullOrEmpty(overrideDomain) && String.IsNullOrEmpty(finalResult))
                    {
                        finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includePort: false)}");
                    }
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, includingTestWww: true, includePort: false)}");
                }

                if (String.IsNullOrEmpty(finalResult))
                {
                    // By passing an empty list to the testDomains parameters, no test domains will be checked.
                    finalResult = await GetSystemObjectValueAsync($"{objectKey}_{HttpContextHelpers.GetHostName(httpContextAccessor?.HttpContext, new List<string>(), true, includePort: false)}");
                }
            }

            if (finalResult != null && finalResult.Contains("{"))
            {
                finalResult = LegacyTemplatesService.DoHttpContextReplacements(finalResult, httpContextAccessor?.HttpContext);
            }

            if (!String.IsNullOrEmpty(finalResult))
            {
                return finalResult;
            }

            if (throwErrorIfEmpty)
            {
                throw new Exception($"System object {objectKey} not found");
            }

            finalResult = defaultResult;

            return finalResult;
        }

        /// <inheritdoc />
        public async Task<string> GetObjectValueAsync(string key, int typeNumber = -100)
        {
            databaseConnection.AddParameter("key", key);

            string query;
            if (typeNumber == -100)
            {
                query = @"SELECT `value` FROM easy_objects WHERE active = 1 AND `key` = ?key";
            }
            else
            {
                databaseConnection.AddParameter("typeNumber", typeNumber);
                query = @"SELECT `value` FROM easy_objects WHERE active = 1 AND `key` = ?key AND typenr = ?typeNumber";
            }

            var dataTable = await databaseConnection.GetAsync(query);
            var result = dataTable.Rows.Count > 0 ? dataTable.Rows[0].Field<string>("value") ?? "" : "";
            return result;
        }

        /// <inheritdoc />
        public Task<string> GetSystemObjectValueAsync(string key)
        {
            return GetObjectValueAsync(key, -1);
        }

        /// <inheritdoc />
        public async Task SetObjectValueAsync(string key, string value, int typeNumber, bool saveHistory = true)
        {
            databaseConnection.ClearParameters();
            databaseConnection.AddParameter("key", key);
            databaseConnection.AddParameter("value", value);
            databaseConnection.AddParameter("typeNumber", typeNumber);
            databaseConnection.AddParameter("saveHistoryGcl", saveHistory); // This is used in triggers.
            var query = """
                        SET @saveHistory = ?saveHistoryGcl;
                        
                        INSERT INTO easy_objects (typenr, `key`, `value`)
                        VALUES (?typeNumber, ?key, ?value)
                        ON DUPLICATE KEY UPDATE `value` = ?value
                        """;

            await databaseConnection.ExecuteAsync(query);
        }

        /// <inheritdoc />
        public async Task SetSystemObjectValueAsync(string key, string value, bool saveHistory = true)
        {
            await SetObjectValueAsync(key, value, -1, saveHistory);
        }
    }
}