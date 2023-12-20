namespace SubmissionCheckSplitter.Application.Helpers;

using Constants;
using Data.Models;

public static class PomDataHelpers
{
    public static IEnumerable<NumberedCsvDataRow> ToNumberedCsvDataRows(this IEnumerable<CsvDataRow> csvDataRows, string submissionPeriod)
    {
        return csvDataRows.Select((row, index) => new NumberedCsvDataRow(index + PomData.RowNumberOffset, submissionPeriod, row));
    }
}