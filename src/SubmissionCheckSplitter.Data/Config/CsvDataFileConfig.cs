namespace SubmissionCheckSplitter.Data.Config;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class CsvDataFileConfig
{
    public const string Section = "CsvDataFile";

    public bool IsLatest { get; set; }
}