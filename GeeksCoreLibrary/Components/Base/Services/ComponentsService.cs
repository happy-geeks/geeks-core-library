using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using GeeksCoreLibrary.Components.Account.Services;
using GeeksCoreLibrary.Components.Base.Interfaces;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Modules.Databases.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Interfaces;
using GeeksCoreLibrary.Modules.GclReplacements.Services;
using GeeksCoreLibrary.Modules.Templates.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace GeeksCoreLibrary.Components.Base.Services;

public class ComponentsService : IComponentsService, IScopedService
{
    private readonly ILogger<AccountsService> logger;
    private readonly IDatabaseConnection databaseConnection;
    private readonly IServiceProvider serviceProvider;
    private readonly IReplacementsMediator replacementsMediator;

    public ComponentsService(ILogger<AccountsService> logger,
        IDatabaseConnection databaseConnection,
        IServiceProvider serviceProvider,
        IReplacementsMediator replacementsMediator)
    {
        this.logger = logger;
        this.databaseConnection = databaseConnection;
        this.serviceProvider = serviceProvider;
        this.replacementsMediator = replacementsMediator;
    }

    /// <inheritdoc />
    public async Task<DataTable> RenderAndExecuteQueryAsync(string queryToUse, Dictionary<string, string> extraDataForReplacements, DataRow dataRowForReplacements = null, bool doVariablesCheck = false, bool skipCache = false)
    {
        if (String.IsNullOrWhiteSpace(queryToUse))
        {
            logger.LogTrace("Query for component is empty!");
            return new DataTable();
        }
        
        if (extraDataForReplacements != null && extraDataForReplacements.Any())
        {
            queryToUse = replacementsMediator.DoReplacements(queryToUse, extraDataForReplacements, true);
        }
        
        await using var templatesScope = serviceProvider.CreateAsyncScope();
        var templatesService = templatesScope.ServiceProvider.GetRequiredService<ITemplatesService>();
        queryToUse = await templatesService.DoReplacesAsync(queryToUse, handleDynamicContent: false, 
            dataRow: dataRowForReplacements, forQuery: true);
        if (doVariablesCheck)
        {
            var expression = new Regex("{.*?}", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(2000));
            if (expression.IsMatch(queryToUse))
            {
                // Don't proceed, query from data selector contains variables, this gives syntax errors.
                return new DataTable();
            }
        }

        return await databaseConnection.GetAsync(queryToUse, skipCache);
    }
}