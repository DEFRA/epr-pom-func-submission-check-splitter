namespace SubmissionCheckSplitter.Data.Config;

using System.ComponentModel.DataAnnotations;

public class ServiceBusConfig
{
    public const string Section = "ServiceBus";

    [Required]
    public string ConnectionString { get; set; }

    [Required]
    public string UploadQueueName { get; set; }

    [Required]
    public string SplitQueueName { get; set; }
}
