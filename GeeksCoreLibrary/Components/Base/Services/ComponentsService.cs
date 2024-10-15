using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Services;
using GeeksCoreLibrary.Components.Base.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.Base.Services;

public class ComponentsService(ILogger<AccountsService> logger, IDatabaseConnection databaseConnection)
    : IComponentsService
{
    private readonly IStringReplacementsService StringReplacementsService;
    private readonly ITemplatesService TemplatesService;

    /// <summary>
    /// Renders the query from the <see param="queryToUse"/> parameter, by replacing all variables with their corresponding values,
    /// then executes that rendered query and lastly returns the <see cref="DataTable" /> with the result(s).
    /// </summary>
    /// <param name="queryToUse">The query to render and execute.</param>
    /// <param name="dataRowForReplacements">Optional: A <see cref="DataRow"/> to use for replacements from the result of a query.</param>
    /// <param name="doVariablesCheck">Optional: If this is set to true and the query still contains unhandled replacements after doing all of the replacements, then the query will not be executed. Default value is <see langword="false" />.</param>
    /// <param name="skipCache">Optional: Set to true to ensure the caching is never used for the query. Default value is <see langword="false" />.</param>
    /// <returns>A <see cref="DataTable" /> with the result(s), or NULL if the query was empty.</returns>
    public async Task<DataTable> RenderAndExecuteQueryAsync(string queryToUse,
        Dictionary<string, string> ExtraDataForReplacements, DataRow dataRowForReplacements = null,
        bool doVariablesCheck = false, bool skipCache = false)
    {
        if (String.IsNullOrWhiteSpace(queryToUse))
        {
            logger.LogTrace("Query for component is empty!");
            return new DataTable();
        }

        if (ExtraDataForReplacements != null && ExtraDataForReplacements.Any())
        {
            queryToUse = StringReplacementsService.DoReplacements(queryToUse, ExtraDataForReplacements, true);
        }

        queryToUse = await TemplatesService.DoReplacesAsync(queryToUse, handleDynamicContent: false,
            dataRow: dataRowForReplacements, forQuery: true);
        if (doVariablesCheck)
        {
            var expression = new Regex("{.*?}", RegexOptions.Compiled | RegexOptions.IgnoreCase,
                TimeSpan.FromMilliseconds(2000));
            if (expression.IsMatch(queryToUse))
            {
                // Don't proceed, query from data selector contains variables, this gives syntax errors.
                return new DataTable();
            }
        }

        return await databaseConnection.GetAsync(queryToUse, skipCache);
    }
}