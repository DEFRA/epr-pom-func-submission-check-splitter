namespace SubmissionCheckSplitter.Data.Models.ValidationDataApi;

public record ValidationDataApiResult(
    string ReferenceNumber,
    bool IsComplianceScheme,
    ICollection<string> MemberOrganisations);