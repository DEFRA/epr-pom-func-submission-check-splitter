namespace SubmissionCheckSplitter.Application.Clients;

public interface ISubmissionApiClient
{
    Task SendReport(
        string blobName,
        string orgId,
        string userId,
        string submissionId,
        int numberOfRecords,
        List<string> errors);
}
