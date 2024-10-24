namespace SubmissionCheckSplitter.Application.Helpers;

using System.Globalization;
using System.Linq;
using System.Reflection;
using ClassMaps;
using CsvHelper;
using CsvHelper.Configuration;
using Data.Attributes;
using Data.Models;
using Exceptions;

public class CsvStreamParser : ICsvStreamParser
{
    private static CsvConfiguration CsvConfiguration => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    };

    public IList<T> GetItemsFromCsvStream<T>(MemoryStream memoryStream, bool isLatest = false)
    {
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        using var csv = new CsvReader(reader, CsvConfiguration);

        try
        {
            csv.Context.RegisterClassMap(new CsvDataRowMap(isLatest));
            csv.Read();
            csv.ReadHeader();

            var header = csv.HeaderRecord;

            if (header != null && (header.SequenceEqual(GetExpectedHeaders().HeaderWith13Columns) || header.SequenceEqual(GetExpectedHeaders().HeadersWith14Columns)))
            {
                return csv.GetRecords<T>().ToList();
            }

            throw new CsvHeaderException("The CSV file header is invalid.");
        }
        catch (Exception ex)
        {
            throw new CsvParseException("Error parsing CSV", ex);
        }
    }

    private static (List<string> HeaderWith13Columns, List<string> HeadersWith14Columns) GetExpectedHeaders()
    {
        var headers = typeof(CsvDataRow).GetProperties()
                .Select(x => x.GetCustomAttribute<ExpectedHeaderAttribute>()?.ExpectedHeader).ToList();

        var name = typeof(CsvDataRow).GetProperty(nameof(CsvDataRow.TransitionalPackagingUnits))
                              .GetCustomAttribute<ExpectedHeaderAttribute>();

        return (HeaderWith13Columns: headers.Where(z => z != name.ExpectedHeader).ToList(), HeadersWith14Columns: headers);
    }
}