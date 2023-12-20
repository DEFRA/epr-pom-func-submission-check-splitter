namespace SubmissionCheckSplitter.UnitTests.Clients;

using AutoFixture;
using AutoFixture.AutoMoq;
using Azure.Messaging.ServiceBus;
using Data.Config;
using Data.Models;
using Data.Models.QueueMessages;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using Newtonsoft.Json;
using SubmissionCheckSplitter.Application.Clients;

[TestClass]
public class ServiceBusQueueClientTests
{
    private readonly IFixture _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    private readonly Mock<IOptions<ServiceBusConfig>> _configMock = new();
    private readonly Mock<ServiceBusClient> _serviceBusClientMock = new();
    private readonly Mock<ServiceBusSender> _serviceBusSenderMock = new();

    [TestInitialize]
    public void Setup()
    {
        var config = _fixture.Create<ServiceBusConfig>();

        _configMock.Setup(x => x.Value).Returns(config);

        _serviceBusClientMock
            .Setup(c => c.CreateSender(config.SplitQueueName))
            .Returns(_serviceBusSenderMock.Object);
    }

    [TestMethod]
    public async Task AddsItemToProducerQueue()
    {
        // arrange
        var sut = new ServiceBusQueueClient(
            _configMock.Object,
            _serviceBusClientMock.Object);

        var producerId = _fixture.Create<string>();
        var blobQueueMessage = _fixture.Create<BlobQueueMessage>();
        var numberedCsvDataRows = _fixture.CreateMany<NumberedCsvDataRow>().ToList();

        // act
        await sut.AddToProducerValidationQueue(
            producerId,
            blobQueueMessage,
            numberedCsvDataRows);

        // assert
        var producerQueueMessage = new ProducerQueueMessage(producerId, blobQueueMessage, numberedCsvDataRows);
        var serializedMessage = JsonConvert.SerializeObject(producerQueueMessage);
        var serviceBusMessage = new ServiceBusMessage(serializedMessage);

        _serviceBusSenderMock
            .Verify(
                x => x.SendMessageAsync(
                It.Is<ServiceBusMessage>(sbm => sbm.Body.ToString() == serviceBusMessage.Body.ToString()),
                It.IsAny<CancellationToken>()),
                Times.Once);
        _serviceBusSenderMock.Verify(x => x.CloseAsync(CancellationToken.None), Times.Once);
    }
}