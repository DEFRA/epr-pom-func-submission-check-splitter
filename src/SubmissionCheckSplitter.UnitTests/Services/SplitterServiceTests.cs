namespace SubmissionCheckSplitter.UnitTests.Services;

using Application.Clients;
using Application.Constants;
using Application.Exceptions;
using Application.Helpers;
using Application.Providers;
using Application.Readers;
using Application.Services;
using AutoFixture;
using AutoFixture.AutoMoq;
using Comparers;
using Data.Config;
using Data.Models;
using Data.Models.QueueMessages;
using Data.Models.SubmissionApi;
using Data.Models.ValidationDataApi;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SubmissionCheckSplitter.Application.Clients.Interfaces;
using SubmissionCheckSplitter.Application.Services.Interfaces;

[TestClass]
public class SplitterServiceTests
{
    private const string SubmissionPeriod = "Mock_Jan to Jun 2023";
    private readonly IFixture _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    private readonly Mock<IDequeueProvider> _dequeueProviderMock = new();
    private readonly Mock<IBlobReader> _blobReaderMock = new();
    private readonly Mock<ICsvStreamParser> _csvHelperMock = new();
    private readonly Mock<IServiceBusQueueClient> _serviceBusQueueClientMock = new();
    private readonly Mock<ISubmissionApiClient> _submissionApiClientMock = new();
    private readonly Mock<IValidationDataApiClient> _validationDataApiClientMock = new();
    private readonly Mock<IIssueCountService> _issueCountServiceMock = new();
    private readonly Mock<ILogger<SplitterService>> _loggerMock = new();
    private readonly Mock<IOptions<ValidationDataApiConfig>> _validationDataApiConfigMock = new();
    private readonly Mock<IOptions<ValidationConfig>> _validationConfigMock = new();
    private SplitterService _systemUnderTest;

    private BlobQueueMessage? _blobQueueMessage;
    private string? _serializedQueueMessage;
    private MemoryStream? _memoryStream;
    private List<CsvDataRow>? _csvItems;

    [TestInitialize]
    public void Setup()
    {
        var csvItemsProducer1 = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, "1")
            .CreateMany(3)
            .ToList();

        var csvItemsProducer2 = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, "2")
            .CreateMany(2)
            .ToList();

        _csvItems = csvItemsProducer1
            .Concat(csvItemsProducer2)
            .ToList();

        _systemUnderTest = new SplitterService(
            _dequeueProviderMock.Object,
            _blobReaderMock.Object,
            _csvHelperMock.Object,
            _serviceBusQueueClientMock.Object,
            _submissionApiClientMock.Object,
            _validationDataApiClientMock.Object,
            _issueCountServiceMock.Object,
            _loggerMock.Object);

        var validationDataApiConfig = new ValidationDataApiConfig { IsEnabled = true };
        _validationDataApiConfigMock.Setup(ap => ap.Value).Returns(validationDataApiConfig);

        var validationConfig = new ValidationConfig { MaxIssuesToProcess = 1000 };
        _validationConfigMock.Setup(ap => ap.Value).Returns(validationConfig);

        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(10);
        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);
        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream))
            .Returns(_csvItems);
    }

    [TestMethod]
    public void ValidCsvData_AddsToQueue_SendsValidReport()
    {
        // Arrange
        string testOrganisationId = "TestOrganisationId"; // Consistent OrganisationId for the test
        string producerId = "123456";
        string producerIdTwo = "234567";
        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _blobQueueMessage.OrganisationId = testOrganisationId; // Set OrganisationId in BlobQueueMessage

        var organisationsResult = new OrganisationsResult(new List<string>() { producerId, producerIdTwo });
        var validationResult = new OrganisationDataResult(producerId, true);
        var membersValidationResult = new OrganisationMembersResult(new List<string> { producerId, producerIdTwo });

        // Setup mock for GetOrganisation
        _validationDataApiClientMock.Setup(x => x.GetOrganisation(testOrganisationId))
            .ReturnsAsync(validationResult);

        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(testOrganisationId, It.IsAny<Guid>()))
            .ReturnsAsync(membersValidationResult);
        _validationDataApiClientMock.Setup(x => x.GetValidOrganisations(organisationsResult.ReferenceNumbers))
            .ReturnsAsync(organisationsResult);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(10);

        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);

        var csvItemsProducer1 = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, producerId)
            .CreateMany(3)
            .ToList();

        var csvItemsProducer2 = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, producerIdTwo)
            .CreateMany(2)
            .ToList();

        _csvItems = csvItemsProducer1
            .Concat(csvItemsProducer2)
            .ToList();

        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream))
            .Returns(_csvItems);

        // Act
        _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _dequeueProviderMock.Verify(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage), Times.Once);
        _blobReaderMock.Verify(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName), Times.Once);
        _csvHelperMock.Verify(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream), Times.Once);

        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    _blobQueueMessage.BlobName,
                    _blobQueueMessage.OrganisationId,
                    _blobQueueMessage.UserId,
                    _blobQueueMessage.SubmissionId,
                    2,
                    It.IsAny<List<CheckSplitterWarning>>(),
                    It.IsAny<List<CheckSplitterError>>(),
                    It.IsAny<List<string>>()),
                Times.Once);

        var numberedCsvItems = _csvItems.ToNumberedCsvDataRows(SubmissionPeriod).ToList();
        var expectedProducer1 = numberedCsvItems.Where(x => x.ProducerId == producerId).ToList();
        var expectedProducer2 = numberedCsvItems.Where(x => x.ProducerId == producerIdTwo).ToList();

        _serviceBusQueueClientMock.Verify(
            x => x.AddToProducerValidationQueue(
                producerId,
                _blobQueueMessage,
                It.Is<List<NumberedCsvDataRow>>(csvDataRows => csvDataRows.SequenceEqual(expectedProducer1, new NumberedCsvDataRowComparer()))),
            Times.Once);

        _serviceBusQueueClientMock.Verify(
            x => x.AddToProducerValidationQueue(
                producerIdTwo,
                _blobQueueMessage,
                It.Is<List<NumberedCsvDataRow>>(csvDataRows => csvDataRows.SequenceEqual(expectedProducer2, new NumberedCsvDataRowComparer()))),
            Times.Once);
    }

    [TestMethod]
    public void InvalidCsvData_SendsInvalidReport()
    {
        // arrange
        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(10);

        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);

        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Throws(new CsvParseException("test"));

        // Act
        _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // assert
        _dequeueProviderMock.Verify(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage), Times.Once);
        _blobReaderMock.Verify(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName), Times.Once);
        _csvHelperMock.Verify(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream), Times.Once);

        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.IsAny<List<CheckSplitterWarning>>(),
                    It.IsAny<List<CheckSplitterError>>(),
                    It.IsAny<List<string>>()),
                Times.Once);

        _serviceBusQueueClientMock
            .Verify(
                x => x.AddToProducerValidationQueue(
                    It.IsAny<string>(),
                    It.IsAny<BlobQueueMessage>(),
                    It.IsAny<List<NumberedCsvDataRow>>()),
                Times.Never);
    }

    [TestMethod]
    public void ProcessServiceBusMessage_LogsError_WhenClientThrowsException()
    {
        // Arrange
        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(10);

        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);

        var submissionApiClientException =
            new SubmissionApiClientException("Error message", new HttpRequestException());
        _submissionApiClientMock
            .Setup(x => x.SendReport(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                null,
                null,
                null))
            .ThrowsAsync(submissionApiClientException);

        _csvItems = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, "1")
            .CreateMany(3)
            .ToList();

        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream))
            .Returns(_csvItems);

        // Act
        _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.VerifyLog(
            logger => logger.LogCritical(
                It.Is<string>(msg => msg.Contains("An unexpected error occurred processing the message")),
                It.IsAny<Exception>()),
            Times.Once);
    }

    [TestMethod]
    public void ProcessServiceBusMessage_LogsError_WhenServiceBus_ClientThrowsException()
    {
        // Arrange
        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(10);

        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);

        _serviceBusQueueClientMock
            .Setup(m =>
                m.AddToProducerValidationQueue(It.IsAny<string>(), _blobQueueMessage, It.IsAny<List<NumberedCsvDataRow>>()))
            .ThrowsAsync(new Exception("Unit test"));

        var csvItems = _fixture
            .Build<CsvDataRow>()
            .With(x => x.ProducerId, "1")
            .CreateMany(3)
            .ToList();

        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream))
            .Returns(csvItems);

        // Act
        _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.VerifyLog(logger => logger.LogCritical("An unexpected error occurred processing the message"));
    }

    [TestMethod]
    public void ProcessServiceBusMessage_AddsErrorAndLogs_WhenCsvFileIsEmpty()
    {
        // Arrange
        _blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock
            .Setup(x => x.GetMessageFromJson<BlobQueueMessage>(_serializedQueueMessage))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream(0);

        _blobReaderMock
            .Setup(x => x.DownloadBlobToStream(_blobQueueMessage.BlobName))
            .Returns(_memoryStream);

        _csvHelperMock
            .Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(_memoryStream))
            .Returns(new List<CsvDataRow>());

        // Act
        _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _submissionApiClientMock.Verify(
            x => x.SendReport(
                _blobQueueMessage.BlobName,
                _blobQueueMessage.OrganisationId,
                _blobQueueMessage.UserId,
                _blobQueueMessage.SubmissionId,
                0,
                It.IsAny<List<CheckSplitterWarning>>(),
                It.IsAny<List<CheckSplitterError>>(),
                It.Is<List<string>>(m => m.Count == 1 && m[0] == ErrorCode.CsvFileEmptyErrorCode)));
        _loggerMock.VerifyLog(logger => logger.LogInformation("The CSV file for submission ID {submissionId} is empty", _blobQueueMessage.SubmissionId));
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_WithValidOrganisationId()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var uploadedProducerIds = new[] { "ValidProducerId" };
        var organisation = new OrganisationDataResult(uploadedProducerIds.First(), false);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = uploadedProducerIds[0] },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _serviceBusQueueClientMock.Verify(
            x =>
            x.AddToProducerValidationQueue(
                It.IsAny<string>(),
                It.Is<BlobQueueMessage>(bqm => bqm.OrganisationId == userOrganisationId),
                It.IsAny<List<NumberedCsvDataRow>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_ThrowsOrganisationNotFoundException_WhenOrganisationNotFound()
    {
        // Arrange
        string organisationId = "TestOrganisationId";
        string blobName = "testBlob";
        _blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = organisationId,
            BlobName = blobName,
            SubmissionPeriod = SubmissionPeriod
        };
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(It.IsAny<string>()))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream();
        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobName))
            .Returns(_memoryStream);

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = "1" }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDataResult("2", false));

        _validationDataApiConfigMock.Setup(config => config.Value)
            .Returns(new ValidationDataApiConfig { IsEnabled = true });

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Organisation does not match")),
                It.IsAny<OrganisationNotFoundException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_WarningAdded_WhenOrganisationNotFoundInMembers()
    {
        // Arrange
        string organisationId = "TestOrganisationId";
        string blobName = "testBlob";
        _blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = organisationId,
            BlobName = blobName,
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = Guid.NewGuid()
        };
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(It.IsAny<string>()))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream();
        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobName))
            .Returns(_memoryStream);

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = "1" }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(It.IsAny<string>()))
            .ReturnsAsync(new OrganisationDataResult("1", true));

        _validationDataApiClientMock
            .Setup(x => x
                .GetOrganisationMembers(It.IsAny<string>(), It.IsAny<Guid>()))
            .ReturnsAsync(new OrganisationMembersResult(new List<string> { "2" }));

        _validationDataApiConfigMock.Setup(config => config.Value)
            .Returns(new ValidationDataApiConfig { IsEnabled = true });

        _issueCountServiceMock.Setup(x => x.GetRemainingIssueCapacityAsync(It.IsAny<string>()))
            .ReturnsAsync(10);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _submissionApiClientMock.Verify(
            x => x.SendReport(
                _blobQueueMessage.BlobName,
                _blobQueueMessage.OrganisationId,
                _blobQueueMessage.UserId,
                _blobQueueMessage.SubmissionId,
                1,
                It.Is<List<CheckSplitterWarning>>(m => m.Count == 1 && m[0].ErrorCodes[0] == ErrorCode.ComplianceSchemeMemberNotFoundErrorCode),
                It.IsAny<List<CheckSplitterError>>(),
                It.IsAny<List<string>>()));
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_HandlesOrganisationNotFoundException()
    {
        // Arrange
        string organisationId = "TestOrganisationId";
        string blobName = "testBlob";
        _blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = organisationId,
            BlobName = blobName,
            SubmissionPeriod = SubmissionPeriod
        };
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(It.IsAny<string>()))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream();
        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobName))
            .Returns(_memoryStream);

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = "1" },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(It.IsAny<string>()))
            .ThrowsAsync(new OrganisationNotFoundException("Organisation does not match"));

        _validationDataApiConfigMock.Setup(config => config.Value)
            .Returns(new ValidationDataApiConfig { IsEnabled = true });

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Organisation does not match")),
                It.IsAny<OrganisationNotFoundException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_HandlesSubmissionApiClientException()
    {
        // Arrange
        _submissionApiClientMock.Setup(x => x.SendReport(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<int>(),
                It.IsAny<List<CheckSplitterWarning>>(),
                It.IsAny<List<CheckSplitterError>>(),
                It.IsAny<List<string>>()))
            .ThrowsAsync(new SubmissionApiClientException("Test Exception"));

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.VerifyLog(logger => logger.LogError(It.IsAny<SubmissionApiClientException>(), It.IsAny<string>()), Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_SkipsOrganisationCheck_WhenValidationDisabled()
    {
        // Arrange
        const string userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var uploadedProducerIds = new[] { "ValidProducerId" };

        _validationDataApiConfigMock.Setup(config => config.Value)
            .Returns(new ValidationDataApiConfig { IsEnabled = false });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = uploadedProducerIds[0] },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _validationDataApiClientMock.Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.Never);
        _validationDataApiClientMock.Verify(x => x.GetOrganisationMembers(It.IsAny<string>(), It.IsAny<Guid>()), Times.Never);
        _serviceBusQueueClientMock.Verify(
            x =>
                x.AddToProducerValidationQueue(
                    It.IsAny<string>(),
                    It.Is<BlobQueueMessage>(bqm => bqm.OrganisationId == userOrganisationId),
                    It.IsAny<List<NumberedCsvDataRow>>()),
            Times.Once);
        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.Is<List<CheckSplitterWarning>>(m => m.Count == 0),
                    It.IsAny<List<CheckSplitterError>>(),
                    It.IsAny<List<string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_PerformsOrganisationCheck_WhenValidationEnabled()
    {
        // Arrange
        const string userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var uploadedProducerIds = new[] { "ValidProducerId" };
        var organisation = new OrganisationDataResult(uploadedProducerIds.First(), true);
        var organisationMembers = new OrganisationMembersResult(new List<string> { uploadedProducerIds.First() });

        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });
        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(organisationMembers);

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = uploadedProducerIds[0] },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _validationDataApiClientMock.Verify(x => x.GetOrganisation(It.IsAny<string>()), Times.AtLeastOnce());
        _validationDataApiClientMock.Verify(
            x => x.GetOrganisationMembers(It.IsAny<string>(), It.IsAny<Guid>()),
            Times.AtLeastOnce());
    }

    [TestMethod]
    public async Task GetOrganisation_MoreThanOneProducerId_ThrowsException()
    {
        // Arrange
        const string organisationId = "TestOrganisationId";
        const string blobName = "testBlob";
        var organisation = new OrganisationDataResult(organisationId, false);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(organisationId))
            .ReturnsAsync(organisation);
        _blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = organisationId,
            BlobName = blobName,
            SubmissionPeriod = SubmissionPeriod
        };
        _serializedQueueMessage = JsonConvert.SerializeObject(_blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(It.IsAny<string>()))
            .Returns(_blobQueueMessage);

        _memoryStream = new MemoryStream();
        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobName))
            .Returns(_memoryStream);

        _csvItems = new List<CsvDataRow>
        {
            new() { ProducerId = "1" },
            new() { ProducerId = "2" }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        _validationDataApiConfigMock.Setup(config => config.Value)
            .Returns(new ValidationDataApiConfig { IsEnabled = true });

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(_serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _loggerMock.Verify(
            x => x.Log(
                LogLevel.Error,
                It.IsAny<EventId>(),
                It.Is<It.IsAnyType>((v, t) => v.ToString().Contains("Organisation does not match")),
                It.IsAny<OrganisationNotFoundException>(),
                It.IsAny<Func<It.IsAnyType, Exception, string>>()),
            Times.Once);
    }

    [TestMethod]
    public async Task GetOrganisationMembers_WithValidUploadedOrgId_ReturnsNoWarnings()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var uploadedProducerIds = new[] { "123456" };
        var organisations = new OrganisationsResult(uploadedProducerIds);
        var organisation = new OrganisationDataResult(userOrganisationId, true);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(new OrganisationMembersResult(uploadedProducerIds));
        _validationDataApiClientMock.Setup(x => x.GetValidOrganisations(uploadedProducerIds))
            .ReturnsAsync(organisations);
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = uploadedProducerIds[0] },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _serviceBusQueueClientMock.Verify(
            x =>
                x.AddToProducerValidationQueue(
                    It.IsAny<string>(),
                    It.Is<BlobQueueMessage>(bqm => bqm.OrganisationId == userOrganisationId),
                    It.IsAny<List<NumberedCsvDataRow>>()),
            Times.Once);
        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.Is<List<CheckSplitterWarning>>(m => m.Count == 0),
                    It.IsAny<List<CheckSplitterError>>(),
                    It.IsAny<List<string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_ErrorAdded_WhenOrganisationIsSixDigitsAndDoesNotExist()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var invalidProducerId = "111111";
        var validProducerId = "123456";
        var validOrganisationMembers = new[] { validProducerId };

        var organisation = new OrganisationDataResult(userOrganisationId, true);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(new OrganisationMembersResult(validOrganisationMembers));
        _validationDataApiClientMock.Setup(x => x.GetValidOrganisations(It.IsAny<IEnumerable<string>>()))
            .ReturnsAsync(new OrganisationsResult(validOrganisationMembers));
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = validProducerId },
            new CsvDataRow { ProducerId = invalidProducerId }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _validationDataApiClientMock.Verify(x => x.GetValidOrganisations(
            It.Is<IEnumerable<string>>(referenceNumbers => referenceNumbers.SequenceEqual(new[] { validProducerId, invalidProducerId }))));
        _submissionApiClientMock.Verify(
            x => x.SendReport(
                blobQueueMessage.BlobName,
                blobQueueMessage.OrganisationId,
                blobQueueMessage.UserId,
                blobQueueMessage.SubmissionId,
                2,
                It.Is<List<CheckSplitterWarning>>(m => m.Count == 1 && m[0].ErrorCodes[0] == ErrorCode.ComplianceSchemeMemberNotFoundErrorCode),
                It.Is<List<CheckSplitterError>>(m => m.Count == 1 && m[0].ErrorCodes[0] == ErrorCode.OrganisationDoesNotExistExistErrorCode),
                It.IsAny<List<string>>()));
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_WhenOrganisationIsSixDigits_SentToValidationDataApi()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var validProducerIds = new[] { "111111" };
        var organisations = new OrganisationsResult(validProducerIds);
        var organisation = new OrganisationDataResult(userOrganisationId, true);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(new OrganisationMembersResult(validProducerIds));
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = validProducerIds[0] }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _validationDataApiClientMock.Verify(
            x => x.GetValidOrganisations(
                organisations.ReferenceNumbers),
            Times.Once());
    }

    [TestMethod]
    public async Task ProcessServiceBusMessage_WhenOrganisationIsNotSixDigits_NeverSentToValidationDataApi_AndNoError()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var invalidProducerIds = new[] { "InvalidProducer" };
        var organisation = new OrganisationDataResult(userOrganisationId, true);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(It.IsAny<OrganisationMembersResult>());
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = invalidProducerIds[0] }
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _validationDataApiClientMock.Verify(
            x => x.GetValidOrganisations(
                It.Is<IEnumerable<string>>(referenceNumbers => referenceNumbers.SequenceEqual(invalidProducerIds))),
            Times.Never);
        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.Is<List<CheckSplitterWarning>>(m => m.Count == 1),
                    It.Is<List<CheckSplitterError>>(m => m.Count == 0),
                    It.IsAny<List<string>>()),
                Times.Once);
    }

    [TestMethod]
    public async Task GetValidOrganisations_WithValidUploadedOrgId_ReturnsNoErrors()
    {
        // Arrange
        var userOrganisationId = "ValidOrgId";
        var complianceSchemeId = Guid.NewGuid();
        var uploadedProducerIds = new[] { "123456" };
        var organisations = new OrganisationsResult(uploadedProducerIds);
        var organisation = new OrganisationDataResult(userOrganisationId, true);

        _validationDataApiClientMock.Setup(x => x.GetOrganisation(userOrganisationId))
            .ReturnsAsync(organisation);
        _validationDataApiClientMock.Setup(x => x.GetOrganisationMembers(userOrganisationId, complianceSchemeId))
            .ReturnsAsync(new OrganisationMembersResult(uploadedProducerIds));
        _validationDataApiClientMock.Setup(x => x.GetValidOrganisations(uploadedProducerIds))
            .ReturnsAsync(new OrganisationsResult(new List<string>() { uploadedProducerIds.First() }));
        _validationDataApiClientMock.Setup(x => x.GetValidOrganisations(uploadedProducerIds))
            .ReturnsAsync(organisations);
        _validationDataApiConfigMock.Setup(config => config.Value).Returns(new ValidationDataApiConfig { IsEnabled = true });

        var blobQueueMessage = new BlobQueueMessage
        {
            OrganisationId = userOrganisationId,
            BlobName = "testBlob",
            SubmissionPeriod = SubmissionPeriod,
            ComplianceSchemeId = complianceSchemeId
        };
        var serializedQueueMessage = JsonConvert.SerializeObject(blobQueueMessage);

        _dequeueProviderMock.Setup(x => x.GetMessageFromJson<BlobQueueMessage>(serializedQueueMessage))
            .Returns(blobQueueMessage);

        _blobReaderMock.Setup(x => x.DownloadBlobToStream(blobQueueMessage.BlobName))
            .Returns(new MemoryStream());

        _csvItems = new List<CsvDataRow>
        {
            new CsvDataRow { ProducerId = uploadedProducerIds[0] },
        };
        _csvHelperMock.Setup(x => x.GetItemsFromCsvStream<CsvDataRow>(It.IsAny<MemoryStream>()))
            .Returns(_csvItems);

        // Act
        await _systemUnderTest.ProcessServiceBusMessage(serializedQueueMessage, _validationDataApiConfigMock.Object, _validationConfigMock.Object);

        // Assert
        _serviceBusQueueClientMock.Verify(
            x =>
                x.AddToProducerValidationQueue(
                    It.IsAny<string>(),
                    It.Is<BlobQueueMessage>(bqm => bqm.OrganisationId == userOrganisationId),
                    It.IsAny<List<NumberedCsvDataRow>>()),
            Times.Once);
        _submissionApiClientMock
            .Verify(
                x => x.SendReport(
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<string>(),
                    It.IsAny<int>(),
                    It.Is<List<CheckSplitterWarning>>(m => m.Count == 0),
                    It.Is<List<CheckSplitterError>>(m => m.Count == 0),
                    It.IsAny<List<string>>()),
                Times.Once);
    }
}