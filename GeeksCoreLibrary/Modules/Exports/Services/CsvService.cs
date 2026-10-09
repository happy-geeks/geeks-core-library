using System;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
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

        foreach (var jToken in data)
        {
            if (jToken is JObject jObject)
            {
                foreach (var pair in jObject)
                {
                    csvWriter.WriteField(pair.Value.ToString());
                }
            }

            csvWriter.NextRecord();
        }

        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public async Task DbDataReaderToCsvStreamAsync(DbDataReader dataReader, Stream outputStream, string delimiter = ";")
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(outputStream);

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter
        };

        await using var streamWriter = new StreamWriter(outputStream, leaveOpen: true);
        await using var csvWriter = new CsvWriter(streamWriter, config);

        // Write column headers from the data reader
        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            csvWriter.WriteField(dataReader.GetName(i));
        }
        await csvWriter.NextRecordAsync();

        // Write data rows by reading from the DbDataReader one at a time
        while (await dataReader.ReadAsync())
        {
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                var value = dataReader.IsDBNull(i) ? string.Empty : dataReader.GetValue(i)?.ToString() ?? string.Empty;
                csvWriter.WriteField(value);
            }
            await csvWriter.NextRecordAsync();
        }

        // Ensure all data is flushed to the stream
        await csvWriter.FlushAsync();
        await streamWriter.FlushAsync();
    }
}