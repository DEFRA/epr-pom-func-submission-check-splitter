namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[ExcludeFromCodeCoverage]
[Serializable]
public class CsvParseException : Exception
{
    public CsvParseException()
    {
    }

    public CsvParseException(string message)
        : base(message)
    {
    }

    public CsvParseException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected CsvParseException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}
