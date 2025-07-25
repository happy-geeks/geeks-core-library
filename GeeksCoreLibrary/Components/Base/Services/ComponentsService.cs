using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Services;
using GeeksCoreLibrary.Components.Base.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using ReplacementRegexes = GeeksCoreLibrary.Modules.GclReplacements.Helpers.PrecompiledRegexes;

namespace GeeksCoreLibrary.Components.Base.Services;

public class ComponentsService(
    ILogger<AccountsService> logger,
    IDatabaseConnection databaseConnection,
    IServiceProvider serviceProvider,
    IReplacementsMediator replacementsMediator)
    : IComponentsService, IScopedService
{
    /// <inheritdoc />
    public async Task<DataTable> RenderAndExecuteQueryAsync(string queryToUse, Dictionary<string, string> extraDataForReplacements, DataRow dataRowForReplacements = null, bool doVariablesCheck = false, bool skipCache = false)
    {
        if (String.IsNullOrWhiteSpace(queryToUse))
        {
            logger.LogTrace("Query for component is empty!");
            return new DataTable();
        }

        if (extraDataForReplacements != null && extraDataForReplacements.Count != 0)
        {
            queryToUse = replacementsMediator.DoReplacements(queryToUse, extraDataForReplacements, true);
        }

        await using var templatesScope = serviceProvider.CreateAsyncScope();
        var templatesService = templatesScope.ServiceProvider.GetRequiredService<ITemplatesService>();
        queryToUse = await templatesService.DoReplacesAsync(queryToUse, handleDynamicContent: false, dataRow: dataRowForReplacements, forQuery: true);
        if (!doVariablesCheck)
        {
            return await databaseConnection.GetAsync(queryToUse, skipCache);
        }
        
        if (ReplacementRegexes.VariableNonCaptureRegex.IsMatch(queryToUse))
        {
            // Don't proceed, query from data selector contains variables, this gives syntax errors.
            return new DataTable();
        }

        return await databaseConnection.GetAsync(queryToUse, skipCache);
    }
}