namespace SubmissionCheckSplitter.Application.Clients;

using System.Text;
using System.Text.Json;
using Data.Config;
using Data.Models.SubmissionApi;
using Exceptions;
using Microsoft.Extensions.Options;

public class SubmissionApiClient : ISubmissionApiClient
{
    private readonly HttpClient _httpClient;
    private readonly StorageAccountConfig _storageAccountConfig;
    private readonly SubmissionApiConfig _submissionApiConfig;

    public SubmissionApiClient(
        HttpClient httpClient,
        IOptions<SubmissionApiConfig> submissionApiConfig,
        IOptions<StorageAccountConfig> storageAccountConfig)
    {
        _httpClient = httpClient;
        _storageAccountConfig = storageAccountConfig.Value;
        _submissionApiConfig = submissionApiConfig.Value;
    }

    public async Task SendReport(
        string blobName,
        string orgId,
        string userId,
        string submissionId,
        int numberOfRecords,
        List<CheckSplitterWarning> warningEventRequest,
        List<CheckSplitterError> errorEventRequest,
        List<string> errors)
    {
        try
        {
            var requestBody = new SubmissionEventRequest(
                numberOfRecords,
                blobName,
                _storageAccountConfig.PomBlobContainerName,
                warningEventRequest,
                errorEventRequest,
                errors);
            var request = BuildRequestMessage(orgId, userId, submissionId, requestBody);

            var response = await _httpClient.SendAsync(request);
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException exception)
        {
            throw new SubmissionApiClientException("A success status code was not received when sending the error report", exception);
        }
    }

    private HttpRequestMessage BuildRequestMessage(string orgId, string userId, string submissionId, SubmissionEventRequest body)
    {
        var uriString = $"{_submissionApiConfig.BaseUrl}/v1/submissions/{submissionId}/events";

        return new HttpRequestMessage
        {
            Method = HttpMethod.Post,
            RequestUri = new Uri(uriString),
            Headers =
            {
                { "organisationId", orgId },
                { "userId", userId }
            },
            Content = new StringContent(JsonSerializer.Serialize(body), Encoding.UTF8, "application/json")
        };
    }
}