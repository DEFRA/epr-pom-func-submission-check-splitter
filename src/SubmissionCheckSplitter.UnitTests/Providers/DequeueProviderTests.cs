namespace SubmissionCheckSplitter.UnitTests.Providers;

using Application.Exceptions;
using AutoFixture;
using AutoFixture.AutoMoq;
using Data.Models.QueueMessages;
using FluentAssertions;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Newtonsoft.Json;
using SubmissionCheckSplitter.Application.Providers;

[TestClass]
public class DequeueProviderTests
{
    private readonly IFixture _fixture = new Fixture()
        .Customize(new AutoMoqCustomization());

    [TestMethod]
    public void Should_throw_exception_when_serialization_unsuccessful()
    {
        // arrange
        var jsonMessage = "{text:message}";
        var dequeueProvider = new DequeueProvider();

        Action act = () => { dequeueProvider.GetMessageFromJson<BlobQueueMessage>(jsonMessage); };

        // act/assert
        act.Should().ThrowExactly<DeserializeQueueException>();
    }

    [TestMethod]
    public void Should_parse_successfully_when_model_matches()
    {
        // arrange
        var blobMessage = _fixture.Create<BlobQueueMessage>();
        var blobMessageJson = JsonConvert.SerializeObject(blobMessage);
        var dequeueProvider = new DequeueProvider();

        // act
        var result = dequeueProvider.GetMessageFromJson<BlobQueueMessage>(blobMessageJson);

        // assert
        result.Should().BeEquivalentTo(blobMessage);
    }
}