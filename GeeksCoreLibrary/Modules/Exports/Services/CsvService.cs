using System;
using System.Data.Common;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
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
            csvWriter.WriteField(EscapeCsvFormulaInjection(pair.Key));
        }

        csvWriter.NextRecord();

        foreach (var jToken in data)
        {
            if (jToken is JObject jObject)
            {
                foreach (var pair in jObject)
                {
                    WriteCsvField(csvWriter, pair.Value);
                }
            }

            csvWriter.NextRecord();
        }

        return stringWriter.ToString();
    }

    /// <inheritdoc />
    public async Task DbDataReaderToCsvStreamAsync(DbDataReader dataReader, Stream outputStream, string delimiter = ";", CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dataReader);
        ArgumentNullException.ThrowIfNull(outputStream);

        if (dataReader.FieldCount == 0)
        {
            return;
        }

        var config = new CsvConfiguration(CultureInfo.InvariantCulture)
        {
            Delimiter = delimiter
        };

        await using var streamWriter = new StreamWriter(outputStream, leaveOpen: true);
        await using var csvWriter = new CsvWriter(streamWriter, config);

        // Write column headers from the data reader
        for (var i = 0; i < dataReader.FieldCount; i++)
        {
            csvWriter.WriteField(EscapeCsvFormulaInjection(dataReader.GetName(i)));
        }
        await csvWriter.NextRecordAsync();

        // Write data rows by reading from the DbDataReader one at a time
        while (await dataReader.ReadAsync(cancellationToken))
        {
            for (var i = 0; i < dataReader.FieldCount; i++)
            {
                if (dataReader.IsDBNull(i))
                {
                    csvWriter.WriteField(String.Empty);
                    continue;
                }

                WriteCsvField(csvWriter, dataReader.GetValue(i));
            }
            await csvWriter.NextRecordAsync();
        }

        // Ensure all data is flushed to the stream
        await csvWriter.FlushAsync();
        await streamWriter.FlushAsync(cancellationToken);
    }

    /// <summary>
    /// Writes a CSV field using invariant culture for non-strings, and guards string-like values against formula injection.
    /// Numeric CLR / JSON types are left unprefixed so values like -19.95 stay numbers.
    /// </summary>
    private static void WriteCsvField(CsvWriter csvWriter, object value)
    {
        if (value == null || value is JValue { Type: JTokenType.Null })
        {
            csvWriter.WriteField(String.Empty);
            return;
        }

        if (value is JValue jValue)
        {
            switch (jValue.Type)
            {
                case JTokenType.Integer:
                case JTokenType.Float:
                    csvWriter.WriteField(Convert.ToString(jValue.Value, CultureInfo.InvariantCulture));
                    return;
                case JTokenType.Boolean:
                case JTokenType.Date:
                case JTokenType.Guid:
                case JTokenType.TimeSpan:
                    csvWriter.WriteField(Convert.ToString(jValue.Value, CultureInfo.InvariantCulture));
                    return;
                default:
                    csvWriter.WriteField(EscapeCsvFormulaInjection(jValue.ToString()));
                    return;
            }
        }

        switch (value)
        {
            case string text:
                csvWriter.WriteField(EscapeCsvFormulaInjection(text));
                break;
            case char:
            case byte:
            case sbyte:
            case short:
            case ushort:
            case int:
            case uint:
            case long:
            case ulong:
            case float:
            case double:
            case decimal:
                csvWriter.WriteField(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            case bool:
            case DateTime:
            case DateTimeOffset:
            case TimeSpan:
            case Guid:
                csvWriter.WriteField(Convert.ToString(value, CultureInfo.InvariantCulture));
                break;
            default:
                csvWriter.WriteField(EscapeCsvFormulaInjection(Convert.ToString(value, CultureInfo.InvariantCulture) ?? String.Empty));
                break;
        }
    }

    /// <summary>
    /// Prefixes values that spreadsheet apps would treat as formulas.
    /// Quotes alone are not enough; Excel/Sheets still evaluate cells starting with these characters.
    /// </summary>
    private static string EscapeCsvFormulaInjection(string value)
    {
        if (String.IsNullOrEmpty(value))
        {
            return value ?? String.Empty;
        }

        var first = value[0];
        if (first is '=' or '+' or '-' or '@' or '\t' or '\r')
        {
            return "'" + value;
        }

        return value;
    }
}
