namespace SubmissionCheckSplitter.UnitTests;

using Data.Config;
using Functions.V1;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using SubmissionCheckSplitter.Application.Services;

[TestClass]
public class ServiceBusQueueTriggerTests
{
    private readonly Mock<ISplitterService> _splitterServiceMock = new();
    private readonly Mock<ILogger<ServiceBusQueueTrigger>> _loggerMock = new();
    private readonly Mock<IOptions<ValidationDataApiConfig>> _validationDataOptionsMock = new();

    private ServiceBusQueueTrigger _systemUnderTest;

    [TestInitialize]
    public void TestInitialize()
    {
        _systemUnderTest = new ServiceBusQueueTrigger(
            _splitterServiceMock.Object,
            _loggerMock.Object,
            _validationDataOptionsMock.Object);
    }

    [TestMethod]
    public async Task RunAsync_NoExceptionThrown_WhenValidPayload()
    {
        // Arrange
        _splitterServiceMock
            .Setup(service => service.ProcessServiceBusMessage(
                It.IsAny<string>(), _validationDataOptionsMock.Object));

        // Act
        _systemUnderTest.RunAsync(It.IsAny<string>());

        // Assert
        _loggerMock.VerifyLog(
            logger => logger.LogInformation(
                It.Is<string>(msg => msg.Contains("Entering RunAsync"))),
            Times.Once);

        _loggerMock.VerifyLog(
            logger => logger.LogInformation(
                It.Is<string>(msg => msg.Contains("Exiting RunAsync"))),
            Times.Once);
    }
}