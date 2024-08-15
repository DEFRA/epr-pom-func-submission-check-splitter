namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
public class OrganisationNotFoundException : Exception
{
    public OrganisationNotFoundException()
    {
    }

    public OrganisationNotFoundException(string message)
        : base(message)
    {
    }

    public OrganisationNotFoundException(string message, Exception inner)
        : base(message, inner)
    {
    }
}