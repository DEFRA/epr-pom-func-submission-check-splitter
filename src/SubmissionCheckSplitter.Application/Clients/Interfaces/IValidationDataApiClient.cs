namespace SubmissionCheckSplitter.Application.Clients.Interfaces;

using Data.Models.ValidationDataApi;

public interface IValidationDataApiClient
{
    Task<OrganisationDataResult> GetOrganisation(string organisationId);

    Task<OrganisationMembersResult> GetOrganisationMembers(string organisationId, Guid? complianceSchemeId);
}