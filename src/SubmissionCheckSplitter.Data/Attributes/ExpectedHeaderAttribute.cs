namespace SubmissionCheckSplitter.Data.Attributes;

using System.Diagnostics.CodeAnalysis;

[ExcludeFromCodeCoverage]
[AttributeUsage(AttributeTargets.Property)]
public class ExpectedHeaderAttribute : Attribute
{
    public ExpectedHeaderAttribute(string expectedHeader)
    {
        ExpectedHeader = expectedHeader;
    }

    public string ExpectedHeader { get; }
}