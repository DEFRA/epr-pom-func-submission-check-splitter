namespace SubmissionCheckSplitter.Application.Services;

using Data.Config;
using Interfaces;
using Microsoft.Extensions.Options;
using StackExchange.Redis;

public class IssueCountService : IIssueCountService
{
    private readonly ValidationConfig _validationOptions;
    private readonly IDatabase _redisDatabase;

    public IssueCountService(
        IConnectionMultiplexer redisConnectionMultiplexer,
        IOptions<ValidationConfig> validationOptions)
    {
        _validationOptions = validationOptions.Value;
        _redisDatabase = redisConnectionMultiplexer.GetDatabase();
    }

    public async Task PersistIssueCountToRedisAsync(string key, int count)
    {
        await _redisDatabase.StringIncrementAsync(key, count);
    }

    public async Task<int> GetRemainingIssueCapacityAsync(string key)
    {
        var redisValue = await _redisDatabase.StringGetAsync(key);
        var currentCount = redisValue.HasValue ? (int)redisValue : 0;
        var remaining = _validationOptions.MaxIssuesToProcess - currentCount;
        return remaining <= 0 ? 0 : remaining;
    }
}