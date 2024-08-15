namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class SubmissionApiClientException : Exception
{
    public SubmissionApiClientException()
    {
    }

    public SubmissionApiClientException(string message)
        : base(message)
    {
    }

    public SubmissionApiClientException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
