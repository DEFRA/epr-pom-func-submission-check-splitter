namespace SubmissionCheckSplitter.Data.Config;

using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class RedisConfig
{
    public const string Section = "Redis";

    [Required]
    public string ConnectionString { get; set; }
}