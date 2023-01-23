using System;
using System.Collections.Generic;
using System.Linq;
using GeeksCoreLibrary.Modules.DataSelector.Models;

namespace GeeksCoreLibrary.Modules.DataSelector.Helpers;

public static class FieldHelpers
{
    /// <summary>
    /// Creates an alias for a field's item detail table JOIN statement.
    /// </summary>
    /// <param name="field">The <see cref="Field"/> object for which this table alias will be created.</param>
    /// <param name="joinIterationCounts">Array containing the current iteration of each level this field's connection has traversed, with the last entry being the iteration of the current connection.</param>
    /// <param name="languageCode">Optional: The language code of the value.</param>
    /// <returns>A combination of the field's name and language code.</returns>
    public static string CreateTableJoinAlias(Field field, IEnumerable<ConnectionIterationModel> joinIterationCounts, string languageCode = null)
    {
        var iterationsPart = joinIterationCounts == null ? "main" : String.Join("_", joinIterationCounts.Select(it => it.Count));
        
        return $"item_{iterationsPart}_detail_{field.FieldName}{(!String.IsNullOrWhiteSpace(languageCode) ? "_" + languageCode : "")}";
    }
}