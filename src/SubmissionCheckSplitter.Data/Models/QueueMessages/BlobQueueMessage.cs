namespace SubmissionCheckSplitter.Data.Models.QueueMessages;

using System.ComponentModel.DataAnnotations;

public class BlobQueueMessage
{
    [Required]
    public string BlobName { get; set; }

    public string SubmissionId { get; set; }

    public string UserId { get; set; }

    public string OrganisationId { get; set; }

    public string SubmissionPeriod { get; set; }
}