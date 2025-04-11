namespace SubmissionCheckSplitter.Data.Config;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsvDataFileConfig
{
    public const string Section = "CsvDataFile";

    public bool EnableTransitionalPackagingUnitsColumn { get; set; }

    public bool EnableRecyclabilityRatingColumn { get; set; }
}