namespace SubmissionCheckSplitter.Application.Clients.Interfaces;

using Data.Models.ValidationDataApi;

public interface IValidationDataApiClient
{
    Task<ValidationDataApiResult> GetOrganisation(string organisationId);
}