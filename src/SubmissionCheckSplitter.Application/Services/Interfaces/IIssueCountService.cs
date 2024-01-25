namespace SubmissionCheckSplitter.Application.Services.Interfaces;

public interface IIssueCountService
{
    Task PersistIssueCountToRedisAsync(string key, int count);

    Task<int> GetRemainingIssueCapacityAsync(string key);
}