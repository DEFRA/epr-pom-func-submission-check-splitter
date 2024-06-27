namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
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
}
