namespace SubmissionCheckSplitter.Application.Services;

using Microsoft.Extensions.Logging;

public static partial class SplitterServiceLoggerMessages
{
    [LoggerMessage(LogLevel.Error, Message = "{Message}")]
    public static partial void ValidationError(this ILogger logger, Exception ex, string message);
}