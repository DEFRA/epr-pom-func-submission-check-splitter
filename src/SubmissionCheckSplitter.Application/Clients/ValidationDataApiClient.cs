namespace SubmissionCheckSplitter.Application.Clients;

using System.Net;
using System.Net.Http.Json;
using Data.Models.ValidationDataApi;
using Exceptions;
using Interfaces;
using Microsoft.Extensions.Logging;

public class ValidationDataApiClient : IValidationDataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ValidationDataApiClient> _logger;

    public ValidationDataApiClient(
        HttpClient httpClient,
        ILogger<ValidationDataApiClient> logger)
    {
        _httpClient = httpClient;
        _logger = logger;
    }

    public async Task<OrganisationDataResult> GetOrganisation(string organisationId)
    {
        try
        {
            var uriString = $"api/organisation/{organisationId}";
            var response = await _httpClient.GetAsync(uriString);

            if (HttpStatusCode.NotFound.Equals(response.StatusCode))
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Organisation details received from Validation Api");

            return await response.Content.ReadFromJsonAsync<OrganisationDataResult>();
        }
        catch (HttpRequestException exception)
        {
            const string message = "A success status code was not received when requesting organisation details";
            _logger.LogError(exception, message);
            throw new ValidationDataApiClientException(message, exception);
        }
    }

    public async Task<OrganisationMembersResult> GetOrganisationMembers(string organisationId, Guid? complianceSchemeId)
    {
        try
        {
            var uriString = $"api/organisation/{organisationId}/members/{complianceSchemeId}";

            var response = await _httpClient.GetAsync(uriString);
            if (HttpStatusCode.NotFound.Equals(response.StatusCode))
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Organisation member details received from Validation Api");

            return await response.Content.ReadFromJsonAsync<OrganisationMembersResult>();
        }
        catch (HttpRequestException exception)
        {
            const string message = "A success status code was not received when requesting organisation member details";
            _logger.LogError(exception, message);
            throw new ValidationDataApiClientException(message, exception);
        }
    }

    public async Task<OrganisationsResult> GetValidOrganisations(IEnumerable<string> referenceNumbers)
    {
        try
        {
            var requestData = new OrganisationsRequest
            {
                ReferenceNumbers = referenceNumbers
            };

            var response = await _httpClient.PostAsJsonAsync($"api/organisations", requestData);

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Valid organisations received from Validation Api");

            return await response.Content.ReadFromJsonAsync<OrganisationsResult>();
        }
        catch (HttpRequestException exception)
        {
            const string message = "A success status code was not received when requesting valid organisations";
            _logger.LogError(exception, message);
            throw new ValidationDataApiClientException(message, exception);
        }
    }
}