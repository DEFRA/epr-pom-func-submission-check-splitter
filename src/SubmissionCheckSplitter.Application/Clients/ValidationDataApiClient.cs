namespace SubmissionCheckSplitter.Application.Clients;

using System.Net;
using System.Net.Http.Json;
using Data.Config;
using Data.Models.ValidationDataApi;
using Exceptions;
using Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

public class ValidationDataApiClient : IValidationDataApiClient
{
    private readonly HttpClient _httpClient;
    private readonly ILogger<ValidationDataApiClient> _logger;
    private readonly ValidationDataApiConfig _validationDataApiConfig;

    public ValidationDataApiClient(
        HttpClient httpClient,
        ILogger<ValidationDataApiClient> logger,
        IOptions<ValidationDataApiConfig> validationDataApiConfig)
    {
        _httpClient = httpClient;
        _logger = logger;
        _validationDataApiConfig = validationDataApiConfig.Value;
    }

    public async Task<ValidationDataApiResult> GetOrganisation(string organisationId)
    {
        try
        {
            var uriString = $"{_validationDataApiConfig.BaseUrl}/api/organisation/{organisationId}";

            var response = await _httpClient.GetAsync(uriString);
            if (HttpStatusCode.NotFound.Equals(response.StatusCode))
            {
                return null;
            }

            response.EnsureSuccessStatusCode();

            _logger.LogInformation("Data received from Validation Api");

            return await response.Content.ReadFromJsonAsync<ValidationDataApiResult>();
        }
        catch (HttpRequestException exception)
        {
            const string message = "A success status code was not received when requesting organisation with member details";
            _logger.LogError(exception, message);
            throw new ValidationDataApiClientException(message, exception);
        }
    }
}