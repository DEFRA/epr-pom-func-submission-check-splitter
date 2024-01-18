namespace SubmissionCheckSplitter.Application.Services.Interfaces;

public interface IIssueCountService
{
    Task IncrementIssueCountAsync(string key, int count);

    Task<int> GetRemainingIssueCapacityAsync(string key);
}