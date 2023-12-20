namespace SubmissionCheckSplitter.Data.Models;

public class NumberedCsvDataRow : CsvDataRow
{
    public NumberedCsvDataRow(int rowNumber, string submissionPeriod, CsvDataRow csvDataRow)
    {
        RowNumber = rowNumber;
        ProducerId = csvDataRow.ProducerId;
        SubsidiaryId = csvDataRow.SubsidiaryId;
        ProducerType = csvDataRow.ProducerType;
        ProducerSize = csvDataRow.ProducerSize;
        DataSubmissionPeriod = csvDataRow.DataSubmissionPeriod;
        WasteType = csvDataRow.WasteType;
        PackagingCategory = csvDataRow.PackagingCategory;
        MaterialType = csvDataRow.MaterialType;
        MaterialSubType = csvDataRow.MaterialSubType;
        FromHomeNation = csvDataRow.FromHomeNation;
        ToHomeNation = csvDataRow.ToHomeNation;
        QuantityKg = csvDataRow.QuantityKg;
        QuantityUnits = csvDataRow.QuantityUnits;
        SubmissionPeriod = submissionPeriod;
    }

    public int RowNumber { get; }

    public string SubmissionPeriod { get; }
}