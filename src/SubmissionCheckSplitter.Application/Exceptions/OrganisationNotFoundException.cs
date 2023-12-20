namespace SubmissionCheckSplitter.Application.Exceptions;

using System.Diagnostics.CodeAnalysis;
using System.Runtime.Serialization;

[ExcludeFromCodeCoverage]
[Serializable]
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

    protected OrganisationNotFoundException(SerializationInfo info, StreamingContext context)
        : base(info, context)
    {
    }
}