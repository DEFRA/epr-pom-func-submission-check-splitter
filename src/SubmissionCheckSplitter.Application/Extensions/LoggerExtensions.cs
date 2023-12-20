namespace SubmissionCheckSplitter.Application.Extensions;

using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

public static class LoggerExtensions
{
    public static void LogEnter<T>(
        this ILogger<T> logger,
        [CallerMemberName]
        string methodName = "")
        => logger.LogInformation("Entering {MethodName}", methodName);

    public static void LogExit<T>(
        this ILogger<T> logger,
        [CallerMemberName]
        string methodName = "")
        => logger.LogInformation("Exiting {MethodName}", methodName);
}