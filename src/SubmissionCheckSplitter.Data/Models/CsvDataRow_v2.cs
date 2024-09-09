namespace SubmissionCheckSplitter.Data.Models;

using System.Diagnostics.CodeAnalysis;
using Attributes;
using CsvHelper.Configuration.Attributes;

[ExcludeFromCodeCoverage]
public class CsvDataRow_v2 : ICsvDataRow
{
    [Index(0)]
    [ExpectedHeader("organisation_id")]
    public string ProducerId { get; init; }

    [Index(1)]
    [ExpectedHeader("subsidiary_id")]
    public string SubsidiaryId { get; init; }

    [Index(2)]
    [ExpectedHeader("organisation_size")]
    public string ProducerSize { get; init; }

    [Index(3)]
    [ExpectedHeader("submission_period")]
    public string DataSubmissionPeriod { get; init; }

    [Index(4)]
    [ExpectedHeader("packaging_activity")]
    public string ProducerType { get; init; }

    [Index(5)]
    [ExpectedHeader("packaging_type")]
    public string WasteType { get; init; }

    [Index(6)]
    [ExpectedHeader("packaging_class")]
    public string PackagingCategory { get; init; }

    [Index(7)]
    [ExpectedHeader("packaging_material")]
    public string MaterialType { get; init; }

    [Index(8)]
    [ExpectedHeader("packaging_material_subtype")]
    public string MaterialSubType { get; init; }

    [Index(9)]
    [ExpectedHeader("from_country")]
    public string FromHomeNation { get; init; }

    [Index(10)]
    [ExpectedHeader("to_country")]
    public string ToHomeNation { get; init; }

    [Index(11)]
    [ExpectedHeader("packaging_material_weight")]
    public string QuantityKg { get; init; }

    [Index(12)]
    [ExpectedHeader("packaging_material_units")]
    public string QuantityUnits { get; init; }

    [Index(13)]
    [ExpectedHeader("previously_paid_packaging_material_units")]
    public string PreviouslyPaidPackagingMaterialUnits { get; init; }
}