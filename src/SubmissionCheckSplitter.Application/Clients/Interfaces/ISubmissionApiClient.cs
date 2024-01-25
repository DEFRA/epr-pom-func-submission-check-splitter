namespace SubmissionCheckSplitter.Application.Clients;

using Data.Models.SubmissionApi;

public interface ISubmissionApiClient
{
    Task SendReport(
        string blobName,
        string orgId,
        string userId,
        string submissionId,
        int numberOfRecords,
        List<CheckSplitterWarning> warningEventRequest,
        List<CheckSplitterError> errorEventRequest,
        List<string> errors);
}