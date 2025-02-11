namespace SubmissionCheckSplitter.Application.Helpers;

using Constants;
using Data.Models;
using SubmissionCheckSplitter.Data.Config;

public static class PomDataHelpers
{
    public static IEnumerable<NumberedCsvDataRow> ToNumberedCsvDataRows(this IEnumerable<CsvDataRow> csvDataRows, string submissionPeriod, CsvDataFileConfig csvDataFileConfigOptions)
    {
        return csvDataRows.Select((row, index) => new NumberedCsvDataRow(index + PomData.RowNumberOffset, submissionPeriod, row, csvDataFileConfigOptions));
    }
}