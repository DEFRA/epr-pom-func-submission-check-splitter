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
using SubmissionCheckSplitter.Data.Config;

public class CsvStreamParser : ICsvStreamParser
{
    private static CsvConfiguration CsvConfiguration => new(CultureInfo.InvariantCulture)
    {
        HasHeaderRecord = true
    };

    public IList<T> GetItemsFromCsvStream<T>(MemoryStream memoryStream, CsvDataFileConfig? csvDataFileConfigOptions)
    {
        memoryStream.Position = 0;
        using var reader = new StreamReader(memoryStream);
        using var csv = new CsvReader(reader, CsvConfiguration);

        try
        {
            csv.Read();
            csv.ReadHeader();
            var header = csv.HeaderRecord;
            var mandatoryHeaders = GetMandatoryHeaders();
            var optionalHeaders = GetOptionalHeaders();

            if (csvDataFileConfigOptions != null)
            {
                if (csvDataFileConfigOptions.EnableTransitionalPackagingUnitsColumn)
                {
                    var headerName = GetHeaderName(nameof(CsvDataRow.TransitionalPackagingUnits));
                    mandatoryHeaders.Add(headerName);
                    optionalHeaders.Remove(headerName);
                }

                if (csvDataFileConfigOptions.EnableRecyclabilityRatingColumn)
                {
                    var headerName = GetHeaderName(nameof(CsvDataRow.RecyclabilityRating));
                    mandatoryHeaders.Add(headerName);
                    optionalHeaders.Remove(headerName);
                }
            }

            if (header != null && (header.Length >= mandatoryHeaders.Count() && header.Where(x => !optionalHeaders.Contains(x)).ToList().SequenceEqual(mandatoryHeaders)))
            {
                csv.Context.RegisterClassMap(new CsvDataRowMap(mandatoryHeaders));
                var result = csv.GetRecords<T>().ToList();
                return result;
            }

            throw new CsvHeaderException("The CSV file header is invalid.");
        }
        catch (Exception ex)
        {
            throw new CsvParseException("Error parsing CSV", ex);
        }
    }

    private static string GetHeaderName(string propertyName)
    {
        return typeof(CsvDataRow).GetProperty(propertyName).GetCustomAttribute<ExpectedHeaderAttribute>()?.ExpectedHeader;
    }

    private static List<string> GetMandatoryHeaders()
    {
        var headers = typeof(CsvDataRow).GetProperties()
            .Where(x => x.GetCustomAttribute<CsvHelper.Configuration.Attributes.OptionalAttribute>() == null).OrderBy(x => x.GetCustomAttribute<CsvHelper.Configuration.Attributes.IndexAttribute>().Index)
                .Select(x => x.GetCustomAttribute<ExpectedHeaderAttribute>()?.ExpectedHeader).ToList();

        return headers;
    }

    private static List<string> GetOptionalHeaders()
    {
        var headers = typeof(CsvDataRow).GetProperties()
            .Where(x => x.GetCustomAttribute<CsvHelper.Configuration.Attributes.OptionalAttribute>() != null).OrderBy(x => x.GetCustomAttribute<CsvHelper.Configuration.Attributes.IndexAttribute>().Index)
                .Select(x => x.GetCustomAttribute<ExpectedHeaderAttribute>()?.ExpectedHeader).ToList();

        return headers;
    }
}