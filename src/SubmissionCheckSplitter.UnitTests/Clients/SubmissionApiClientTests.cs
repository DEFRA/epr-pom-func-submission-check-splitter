namespace SubmissionCheckSplitter.UnitTests.Clients;

using System.Net;
using Application.Clients;
using Data.Config;
using Data.Models.SubmissionApi;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;

[TestClass]
public class SubmissionApiClientTests
{
    private const string ContainerName = "pom-blob-container-name";
    private const string BlobName = "blob";
    private const string OrgId = "org";
    private const string UserId = "user";
    private const string SubmissionId = "sub";
    private const string InvalidErrorCode = "82";
    private const string InvalidMessage = "Invalid CSV";
    private const int ProducerCount = 100;

    private readonly Mock<IOptions<SubmissionApiConfig>> _submissionApiOptionsMock = new();
    private readonly Mock<IOptions<StorageAccountConfig>> _storageAccountOptionsMock = new();
    private SubmissionApiConfig? _submissionApiConfig;
    private StorageAccountConfig? _storageAccountConfig;

    [TestInitialize]
    public void Setup()
    {
        _submissionApiConfig = new SubmissionApiConfig { BaseUrl = "https://www.testurl.com" };
        _storageAccountConfig = new StorageAccountConfig { PomBlobContainerName = ContainerName };
        _submissionApiOptionsMock.Setup(x => x.Value).Returns(_submissionApiConfig);
        _storageAccountOptionsMock.Setup(x => x.Value).Returns(_storageAccountConfig);
    }

    [TestMethod]
    public async Task SendsValidReport()
    {
        // arrange
        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage { StatusCode = HttpStatusCode.OK })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_submissionApiConfig!.BaseUrl) };

        var sut = new SubmissionApiClient(httpClient, _submissionApiOptionsMock.Object, _storageAccountOptionsMock.Object);

        // act
        await sut.SendReport(BlobName, OrgId, UserId, SubmissionId, ProducerCount, null, null, null);

        // assert
        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(
                req => IsExpectedRequestMessage(req)
                       && IsValidReport(req.Content)),
            ItExpr.IsAny<CancellationToken>());
    }

    [TestMethod]
    public async Task SendsInvalidReport()
    {
        // arrange
        var errors = new List<string> { InvalidErrorCode, InvalidMessage };

        var handlerMock = new Mock<HttpMessageHandler>(MockBehavior.Strict);
        handlerMock
            .Protected()
            .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.IsAny<HttpRequestMessage>(),
                ItExpr.IsAny<CancellationToken>())
            .ReturnsAsync(new HttpResponseMessage
            {
                StatusCode = HttpStatusCode.OK,
                Content = new StringContent("[{'id':1,'value':'1'}]")
            })
            .Verifiable();

        var httpClient = new HttpClient(handlerMock.Object) { BaseAddress = new Uri(_submissionApiConfig!.BaseUrl) };

        var sut = new SubmissionApiClient(httpClient, _submissionApiOptionsMock.Object, _storageAccountOptionsMock.Object);

        await sut.SendReport(BlobName, OrgId, UserId, SubmissionId, 0, null, null, errors);

        handlerMock.Protected().Verify(
            "SendAsync",
            Times.Exactly(1),
            ItExpr.Is<HttpRequestMessage>(
                req => IsExpectedRequestMessage(req)
                       && IsInvalidReport(req.Content)),
            ItExpr.IsAny<CancellationToken>());
    }

    private static bool IsValidReport(HttpContent? content)
    {
        if (content is null)
        {
            return false;
        }

        var stringContent = content.ReadAsStringAsync().Result;
        var report = JsonConvert.DeserializeObject<SubmissionEventRequest>(stringContent);

        if (report is null)
        {
            return false;
        }

        return (report.Errors == null || report.Errors.Count == 0)
               && report is
               {
                   DataCount: ProducerCount,
                   BlobContainerName: ContainerName
               };
    }

    private static bool IsInvalidReport(HttpContent? content)
    {
        return !IsValidReport(content);
    }

    private bool IsExpectedRequestMessage(HttpRequestMessage req)
    {
        var expectedUri = new Uri($"{_submissionApiConfig!.BaseUrl}/v1/submissions/{SubmissionId}/events");

        return req.Method == HttpMethod.Post
               && req.RequestUri == expectedUri
               && req.Headers.GetValues("organisationId").Contains(OrgId)
               && req.Headers.GetValues("userId").Contains(UserId);
    }
}