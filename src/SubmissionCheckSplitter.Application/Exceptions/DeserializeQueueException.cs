namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[Serializable]
public class DeserializeQueueException : Exception
{
    public DeserializeQueueException()
    {
    }

    public DeserializeQueueException(string message)
        : base(message)
    {
    }

    public DeserializeQueueException(string message, Exception inner)
        : base(message, inner)
    {
    }
}
