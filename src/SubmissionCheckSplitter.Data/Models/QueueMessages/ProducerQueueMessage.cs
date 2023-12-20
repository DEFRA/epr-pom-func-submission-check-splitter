namespace SubmissionCheckSplitter.Data.Models.QueueMessages;

public class ProducerQueueMessage
{
    public ProducerQueueMessage(string producerId, BlobQueueMessage blobQueueMessage, IEnumerable<NumberedCsvDataRow> numberedCsvDataRows)
    {
        ProducerId = producerId;
        BlobName = blobQueueMessage.BlobName;
        SubmissionId = blobQueueMessage.SubmissionId;
        UserId = blobQueueMessage.UserId;
        OrganisationId = blobQueueMessage.OrganisationId;
        Rows = numberedCsvDataRows;
    }

    public string ProducerId { get; }

    public string BlobName { get; set; }

    public string SubmissionId { get; }

    public string UserId { get; }

    public string OrganisationId { get; }

    public IEnumerable<NumberedCsvDataRow> Rows { get; }
}
