namespace SubmissionCheckSplitter.UnitTests.Comparers;

using Data.Models;

public class NumberedCsvDataRowComparer : IEqualityComparer<NumberedCsvDataRow>
{
    public bool Equals(NumberedCsvDataRow? x, NumberedCsvDataRow? y)
    {
        return x != null && y != null
                         && x.RowNumber == y.RowNumber
                         && x.ProducerId == y.ProducerId
                         && x.ProducerType == y.ProducerType
                         && x.ProducerSize == y.ProducerSize
                         && x.SubsidiaryId == y.SubsidiaryId
                         && x.DataSubmissionPeriod == y.DataSubmissionPeriod
                         && x.WasteType == y.WasteType
                         && x.PackagingCategory == y.PackagingCategory
                         && x.MaterialType == y.MaterialType
                         && x.MaterialSubType == y.MaterialSubType
                         && x.FromHomeNation == y.FromHomeNation
                         && x.ToHomeNation == y.ToHomeNation
                         && x.QuantityKg == y.QuantityKg
                         && x.QuantityUnits == y.QuantityUnits
                         && x.TransitionalPackagingUnits == y.TransitionalPackagingUnits
                         && x.RecyclabilityRating == y.RecyclabilityRating;
    }

    public int GetHashCode(NumberedCsvDataRow obj) => obj.GetHashCode();
}