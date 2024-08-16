namespace SubmissionCheckSplitter.Application.Exceptions;

using System;
using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class ValidationDataApiClientException : Exception
{
    public ValidationDataApiClientException()
    {
    }

    public ValidationDataApiClientException(string message)
        : base(message)
    {
    }

    public ValidationDataApiClientException(string message, Exception inner)
        : base(message, inner)
    {
    }
}