namespace SubmissionCheckSplitter.UnitTests.Clients;

using System.Net;
using Application.Exceptions;
using Data.Config;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using SubmissionCheckSplitter.Application.Clients;

[TestClass]
public class ValidationDataApiClientTests
{
    private readonly Mock<IOptions<ValidationDataApiConfig>> _validationDataApiOptionsMock = new();
    private Mock<HttpMessageHandler> _handlerMock;
    private ValidationDataApiClient _validationDataApiClient;

    [TestInitialize]
    public void Setup()
    {
        _handlerMock = new Mock<HttpMessageHandler>();
        var validationDataApiConfig = new ValidationDataApiConfig
        {
            BaseUrl = "https://www.testurl.com",
            ClientId = Guid.NewGuid().ToString(),
            IsEnabled = true,
            Timeout = 10
        };
        _validationDataApiOptionsMock.Setup(x => x.Value).Returns(validationDataApiConfig);
        var httpClient = new HttpClient(_handlerMock.Object)
        {
            BaseAddress = new Uri(validationDataApiConfig.BaseUrl),
        };

        _validationDataApiClient = new ValidationDataApiClient(
            httpClient,
            new NullLogger<ValidationDataApiClient>(),
            _validationDataApiOptionsMock.Object);
    }

    [TestMethod]
    public async Task GetOrganisation_SuccessfulResponse()
    {
        // Arrange
        const string organisationId = "test-org-id";
        const string mockJson = @"
            {
                ""ReferenceNumber"": ""ref-12345"",
                ""IsComplianceScheme"": true,
                ""MemberOrganisations"": [""Org1"", ""Org2"", ""Org3""]
            }";
        var mockResponse = new HttpResponseMessage
        {
            StatusCode = HttpStatusCode.OK,
            Content = new StringContent(mockJson),
        };

        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(req =>
                    req.Method == HttpMethod.Get &&
                    req.RequestUri.ToString().EndsWith($"organisation/{organisationId}")),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(mockResponse);

        // Act
        var result = await _validationDataApiClient.GetOrganisation(organisationId);

        // Assert
        result.Should().NotBeNull();
        result.ReferenceNumber.Should().Be("ref-12345");
        result.IsComplianceScheme.Should().BeTrue();
        result.MemberOrganisations.Should().BeEquivalentTo("Org1", "Org2", "Org3");
        _handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(req =>
                req.Method == HttpMethod.Get &&
                req.RequestUri.ToString().EndsWith($"organisation/{organisationId}") &&
                mockResponse.StatusCode == HttpStatusCode.OK),
            ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task GetOrganisation_NotFoundResponse()
    {
        // Arrange
        var organisationId = "non-existent-org";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.NotFound
            });

        // Act
        var result = await _validationDataApiClient.GetOrganisation(organisationId);

        // Assert
        result.Should().BeNull();
    }

    [TestMethod]
    public async Task GetOrganisation_BadRequestResponse()
    {
        // Arrange
        var organisationId = "bad-request-org";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.BadRequest
            });

        // Act
        Func<Task> act = async () => await _validationDataApiClient.GetOrganisation(organisationId);

        // Assert
        await act.Should().ThrowExactlyAsync<ValidationDataApiClientException>();
    }

    [TestMethod]
    public async Task GetOrganisation_ServerErrorResponse()
    {
        // Arrange
        var organisationId = "server-error-org";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.InternalServerError
            });

        // Act
        Func<Task> act = async () => await _validationDataApiClient.GetOrganisation(organisationId);

        // Assert
        await act.Should().ThrowExactlyAsync<ValidationDataApiClientException>();
    }

    [TestMethod]
    public async Task GetOrganisation_ThrowsException()
    {
        // Arrange
        var organisationId = "exception-org";
        _handlerMock.Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ThrowsAsync(new HttpRequestException());

        // Act
        Func<Task> act = async () => await _validationDataApiClient.GetOrganisation(organisationId);

        // Assert
        await act.Should().ThrowExactlyAsync<ValidationDataApiClientException>();
    }
}