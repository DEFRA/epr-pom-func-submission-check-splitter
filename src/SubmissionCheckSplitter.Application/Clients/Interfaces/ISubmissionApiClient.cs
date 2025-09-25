namespace SubmissionCheckSplitter.Application.Clients;

using Data.Models.SubmissionApi;

public interface ISubmissionApiClient
{
#pragma warning disable S107 // Methods should not have too many parameters
    Task SendReport(
        string blobName,
        string orgId,
        string userId,
        string submissionId,
        int numberOfRecords,
        List<CheckSplitterWarning> warningEventRequest,
        List<CheckSplitterError> errorEventRequest,
        List<string> errors);
#pragma warning restore S107 // Methods should not have too many parameters
}