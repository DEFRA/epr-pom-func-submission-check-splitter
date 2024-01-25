namespace SubmissionCheckSplitter.UnitTests.Services;

using FluentAssertions;
using Microsoft.Extensions.Options;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Moq;
using StackExchange.Redis;
using SubmissionCheckSplitter.Application.Services.Interfaces;
using SubmissionCheckSplitter.Data.Config;

[TestClass]
public class IssueCountServiceTests
{
     private const string MockKey = "mock-key";
     private const int MaxIssuesToProcess = 1000;
     private const int IssuesToProcess = 100;

     private Mock<IConnectionMultiplexer> _connectionMultiplexerMock = new();
     private Mock<IDatabase> _databaseMock = new();
     private Mock<IOptions<ValidationConfig>> _validationOptionsMock = new();
     private IIssueCountService _serviceUnderTest;

     [TestInitialize]
     public void TestInitialize()
    {
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), default))
            .ReturnsAsync(IssuesToProcess);
        _validationOptionsMock.Setup(x => x.Value)
            .Returns(new ValidationConfig { MaxIssuesToProcess = MaxIssuesToProcess });
        _connectionMultiplexerMock
            .Setup(x => x.GetDatabase(It.IsAny<int>(), default))
            .Returns(_databaseMock.Object);

        _serviceUnderTest = new SubmissionCheckSplitter.Application.Services.IssueCountService(_connectionMultiplexerMock.Object, _validationOptionsMock.Object);
    }

     [TestMethod]
     public async Task IncrementIssueCountAsync_WhenCalled_SuccessfullyCallsToIncrementCount()
    {
        // Arrange
        const int mockCount = 0;

        // Act
        _serviceUnderTest.PersistIssueCountToRedisAsync(MockKey, mockCount);

        // Assert
        _databaseMock.Verify(x => x.StringIncrementAsync(MockKey, mockCount, default), Times.Once);
    }

     [TestMethod]
     public async Task GetRemainingIssueCapacityAsync_WhenCalledWithAKeyThatHasValueGreaterThanZeroButLessThanMaxIssues_ReturnsTheDifferenceWithMaxIssuesToProcess()
    {
        // Act
        var result = await _serviceUnderTest.GetRemainingIssueCapacityAsync(MockKey);

        // Assert
        result.Should().Be(MaxIssuesToProcess - IssuesToProcess);
    }

     [TestMethod]
     public async Task GetRemainingIssueCapacityAsync_WhenCalledWithAKeyThatHasValueEqualToZero_ReturnsMaxIssuesToProcess()
    {
        // Arrange
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), default))
            .ReturnsAsync(0);

        // Act
        var result = await _serviceUnderTest.GetRemainingIssueCapacityAsync(MockKey);

        // Assert
        result.Should().Be(MaxIssuesToProcess);
    }

     [TestMethod]
     public async Task GetRemainingIssueCapacityAsync_WhenCalledWithAKeyThatHasValueGreaterThanMaxIssues_ReturnsZero()
    {
        // Arrange
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), default))
            .ReturnsAsync(MaxIssuesToProcess + 1);

        // Act
        var result = await _serviceUnderTest.GetRemainingIssueCapacityAsync(MockKey);

        // Assert
        result.Should().Be(0);
    }

     [TestMethod]
     public async Task GetRemainingIssueCapacityAsync_WhenCalledWithAKeyHasNoValue_ReturnsMaxCount()
    {
        // Arrange
        _databaseMock.Setup(x => x.StringGetAsync(It.IsAny<RedisKey>(), default));

        // Act
        var result = await _serviceUnderTest.GetRemainingIssueCapacityAsync(MockKey);

        // Assert
        result.Should().Be(1000);
    }
}