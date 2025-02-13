namespace SubmissionCheckSplitter.Data.Models;

using SubmissionCheckSplitter.Data.Config;

public class NumberedCsvDataRow : CsvDataRow
{
    public NumberedCsvDataRow(int rowNumber, string submissionPeriod, CsvDataRow csvDataRow, CsvDataFileConfig csvDataFileConfigOptions)
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
        TransitionalPackagingUnits = csvDataRow.TransitionalPackagingUnits;
        RecyclabilityRating = csvDataRow.RecyclabilityRating;
        IsLatest = csvDataFileConfigOptions.EnableTransitionalPackagingUnitsColumn;
        IsRecyclabilityRatingRequired = csvDataFileConfigOptions.EnableRecyclabilityRatingColumn;
    }

    public int RowNumber { get; }

    public string SubmissionPeriod { get; }

    public bool IsLatest { get; set; }

    public bool IsRecyclabilityRatingRequired { get; set; }

    public override bool ShouldSerializeTransitionalPackagingUnits()
    {
        return IsLatest;
    }

    public override bool ShouldSerializeRecyclabilityRating()
    {
        return IsRecyclabilityRatingRequired;
    }
}