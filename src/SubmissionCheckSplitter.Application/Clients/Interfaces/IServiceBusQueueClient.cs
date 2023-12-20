namespace SubmissionCheckSplitter.Application.Clients.Interfaces;

using SubmissionCheckSplitter.Data.Models;
using SubmissionCheckSplitter.Data.Models.QueueMessages;

public interface IServiceBusQueueClient
{
    Task AddToProducerValidationQueue(
        string producerId,
        BlobQueueMessage blobQueueMessage,
        List<NumberedCsvDataRow> numberedCsvDataRows);
}