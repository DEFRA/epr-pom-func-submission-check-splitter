namespace SubmissionCheckSplitter.Data.Config;

using System.ComponentModel.DataAnnotations;

public class StorageAccountConfig
{
    public const string Section = "StorageAccount";

    [Required]
    public string ConnectionString { get; set; }

    [Required]
    public string PomBlobContainerName { get; set; }
}
