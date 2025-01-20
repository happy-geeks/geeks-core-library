using System;
using System.Globalization;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using GeeksCoreLibrary.Core.DependencyInjection.Interfaces;
using GeeksCoreLibrary.Core.Helpers;
using GeeksCoreLibrary.Modules.Exports.Interfaces;
using Newtonsoft.Json.Linq;

namespace GeeksCoreLibrary.Modules.Exports.Services;

public class CsvService : ICsvService, IScopedService
{
    /// <inheritdoc />
    public string JsonArrayToCsv(JArray data, string delimiter = ";")
    {
        if (data == null || !data.Any())
        {
            return String.Empty;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter
        };
        using var stringWriter = new StringWriter();
        using var csvWriter = new CsvWriter(stringWriter, config);

        data = JsonHelpers.FlattenJsonArray(data);

        foreach (var pair in data.Cast<JObject>().First())
        {
            csvWriter.WriteField(pair.Key);
        }

        csvWriter.NextRecord();

        foreach (JObject row in data)
        {
            foreach (var pair in row)
            {
                csvWriter.WriteField(pair.Value.ToString());
            }

            csvWriter.NextRecord();
        }

        return stringWriter.ToString();
    }
}