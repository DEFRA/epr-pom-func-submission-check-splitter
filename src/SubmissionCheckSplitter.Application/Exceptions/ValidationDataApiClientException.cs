namespace SubmissionCheckSplitter.Application.Exceptions;

using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[ExcludeFromCodeCoverage]
[Serializable]
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

    protected ValidationDataApiClientException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}