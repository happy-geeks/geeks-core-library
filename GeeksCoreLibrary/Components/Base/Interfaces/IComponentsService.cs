using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

namespace GeeksCoreLibrary.Components.Base.Interfaces;

public interface IComponentsService
{
    /// <summary>
    /// Renders the query from the <see param="queryToUse"/> parameter, by replacing all variables with their corresponding values,
    /// then executes that rendered query and lastly returns the <see cref="DataTable" /> with the result(s).
    /// </summary>
    /// <param name="queryToUse">The query to render and execute.</param>
    /// <param name="dataRowForReplacements">Optional: A <see cref="DataRow"/> to use for replacements from the result of a query.</param>
    /// <param name="doVariablesCheck">Optional: If this is set to true and the query still contains unhandled replacements after doing all of the replacements, then the query will not be executed. Default value is <see langword="false" />.</param>
    /// <param name="skipCache">Optional: Set to true to ensure the caching is never used for the query. Default value is <see langword="false" />.</param>
    /// <returns>A <see cref="DataTable" /> with the result(s), or NULL if the query was empty.</returns>
    Task<DataTable> RenderAndExecuteQueryAsync(string queryToUse, Dictionary<string, string> ExtraDataForReplacements, DataRow dataRowForReplacements = null, bool doVariablesCheck = false, bool skipCache = false);
}