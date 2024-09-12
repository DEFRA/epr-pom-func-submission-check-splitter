namespace SubmissionCheckSplitter.Data.Models;

public class NumberedCsvDataRow : CsvDataRow
{
    public NumberedCsvDataRow(int rowNumber, string submissionPeriod, CsvDataRow csvDataRow, bool isLatest)
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
        PreviouslyPaidPackagingMaterialUnits = csvDataRow.PreviouslyPaidPackagingMaterialUnits;
        IsLatest = isLatest;
    }

    public int RowNumber { get; }

    public string SubmissionPeriod { get; }

    public bool IsLatest { get; set; }

    public override bool ShouldSerializePreviouslyPaidPackagingMaterialUnits()
    {
        return IsLatest;
    }
}