namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[ExcludeFromCodeCoverage]
[Serializable]
public class CsvFileEmptyValueException : Exception
{
    public CsvFileEmptyValueException()
    {
    }

    public CsvFileEmptyValueException(string message)
        : base(message)
    {
    }

    public CsvFileEmptyValueException(string message, Exception inner)
        : base(message, inner)
    {
    }

    protected CsvFileEmptyValueException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}