namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[ExcludeFromCodeCoverage]
[Serializable]
public class CsvHeaderException : Exception
{
    public CsvHeaderException()
    {
    }

    public CsvHeaderException(string message)
        : base(message)
    {
    }

    public CsvHeaderException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected CsvHeaderException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}