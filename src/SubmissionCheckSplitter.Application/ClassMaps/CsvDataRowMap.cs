namespace SubmissionCheckSplitter.Application.ClassMaps;

using System.Diagnostics.CodeAnalysis;
using CsvHelper.Configuration;
using Data.Models;
using TypeConverters;

[ExcludeFromCodeCoverage]
public class CsvDataRowMap : ClassMap<CsvDataRow>
{
    public CsvDataRowMap()
    {
        Map(x => x.ProducerId).Index(0).TypeConverter<StringConverter>();
        Map(x => x.SubsidiaryId).Index(1).TypeConverter<StringConverter>();
        Map(x => x.ProducerSize).Index(2).TypeConverter<StringConverter>();
        Map(x => x.DataSubmissionPeriod).Index(3).TypeConverter<StringConverter>();
        Map(x => x.ProducerType).Index(4).TypeConverter<StringConverter>();
        Map(x => x.WasteType).Index(5).TypeConverter<StringConverter>();
        Map(x => x.PackagingCategory).Index(6).TypeConverter<StringConverter>();
        Map(x => x.MaterialType).Index(7).TypeConverter<StringConverter>();
        Map(x => x.MaterialSubType).Index(8).TypeConverter<StringConverter>();
        Map(x => x.FromHomeNation).Index(9).TypeConverter<StringConverter>();
        Map(x => x.ToHomeNation).Index(10).TypeConverter<StringConverter>();
        Map(x => x.QuantityKg).Index(11).TypeConverter<StringConverter>();
        Map(x => x.QuantityUnits).Index(12).TypeConverter<StringConverter>();
        Map(x => x.PreviouslyPaidPackagingMaterialUnits).Index(13).TypeConverter<StringConverter>();
    }
}