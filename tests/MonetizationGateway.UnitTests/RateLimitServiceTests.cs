using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using MonetizationGateway.Configuration;
using MonetizationGateway.Models;
using MonetizationGateway.Services;
using Moq;
using StackExchange.Redis;
using Xunit;

namespace MonetizationGateway.UnitTests;

public class RateLimitServiceTests
{
    private static TierConfig FreeTier => new() { MonthlyQuota = 1000, RequestsPerSecond = 2, MonthlyPriceUsd = 0 };
    private static TierConfig ProTier => new() { MonthlyQuota = 100_000, RequestsPerSecond = 10, MonthlyPriceUsd = 50 };

    private static RateLimitService CreateService(IConnectionMultiplexer redis, RateLimitOptions? options = null)
    {
        options ??= new RateLimitOptions();
        return new RateLimitService(redis, Options.Create(options), NullLogger<RateLimitService>.Instance);
    }

    private static (Mock<IConnectionMultiplexer>, Mock<IDatabase>) CreateRedisMocks()
    {
        var dbMock = new Mock<IDatabase>();
        var redisMock = new Mock<IConnectionMultiplexer>();
        redisMock.Setup(r => r.GetDatabase(It.IsAny<int>(), It.IsAny<object>())).Returns(dbMock.Object);
        return (redisMock, dbMock);
    }

    [Fact]
    public async Task CheckAndConsumeReqSecAsync_WhenUnderLimit_AllowsRequest()
    {
        // Arrange
        var (redisMock, dbMock) = CreateRedisMocks();
        SetupDbForUnderLimit(dbMock);
        var service = CreateService(redisMock.Object);

        // Act
        var result = await service.CheckAndConsumeReqSecAsync(1, FreeTier);

        // Assert
        result.Allowed.Should().BeTrue();
        result.Limit.Should().Be(1000);
        dbMock.Verify(d => d.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndConsumeReqSecAsync_WhenAtReqPerSecondLimit_DeniesRequest()
    {
        // Arrange
        var (redisMock, dbMock) = CreateRedisMocks();
        SetupDbForAtReqSecLimit(dbMock);
        var service = CreateService(redisMock.Object);

        // Act
        var result = await service.CheckAndConsumeReqSecAsync(1, FreeTier);

        // Assert
        result.Allowed.Should().BeFalse();
        result.RetryAfterSeconds.Should().Be(1);
        result.IsQuotaExceeded.Should().BeFalse();
    }

    [Fact]
    public async Task CheckAndConsumeReqSecAsync_WhenMonthlyQuotaExceeded_DeniesRequest()
    {
        // Arrange
        var (redisMock, dbMock) = CreateRedisMocks();
        SetupDbForQuotaExceeded(dbMock);
        var service = CreateService(redisMock.Object);

        // Act
        var result = await service.CheckAndConsumeReqSecAsync(1, FreeTier);

        // Assert
        result.Allowed.Should().BeFalse();
        result.IsQuotaExceeded.Should().BeTrue();
    }

    [Fact]
    public async Task CheckAndConsumeReqSecAsync_WhenProTier_AllowsMoreReqPerSecondThanFree()
    {
        // Arrange
        var (redisMock, dbMock) = CreateRedisMocks();
        SetupDbForAtReqSecLimit(dbMock);
        var service = CreateService(redisMock.Object);

        // Act
        var resultFree = await service.CheckAndConsumeReqSecAsync(1, FreeTier);
        dbMock.Invocations.Clear();
        SetupDbForAtReqSecLimit(dbMock);
        var resultPro = await service.CheckAndConsumeReqSecAsync(2, ProTier);

        // Assert
        resultFree.Allowed.Should().BeFalse();
        resultPro.Allowed.Should().BeTrue();
    }

    [Fact]
    public async Task IncrementMonthlyQuotaAsync_WhenCalled_IncrementsRedisKey()
    {
        // Arrange
        var (redisMock, dbMock) = CreateRedisMocks();
        dbMock.Setup(d => d.StringIncrementAsync(It.IsAny<RedisKey>(), It.IsAny<long>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync(1);
        dbMock.Setup(d => d.KeyTimeToLiveAsync(It.IsAny<RedisKey>(), It.IsAny<CommandFlags>()))
            .ReturnsAsync((TimeSpan?)null);
        dbMock.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<CommandFlags>()))
            .Returns(Task.FromResult(true));
        var service = CreateService(redisMock.Object);

        // Act
        await service.IncrementMonthlyQuotaAsync(42);

        // Assert
        dbMock.Verify(d => d.StringIncrementAsync(It.Is<RedisKey>(k => k.ToString().Contains("42")), It.IsAny<long>(), It.IsAny<CommandFlags>()), Times.Once);
    }

    [Fact]
    public async Task CheckAndConsumeReqSecAsync_WhenQuotaExceededButEnableQuotaCheckingFalse_AllowsRequest()
    {
        // Arrange: quota would be exceeded, but option disables quota check
        var (redisMock, dbMock) = CreateRedisMocks();
        SetupDbForQuotaExceeded(dbMock);
        var options = new RateLimitOptions { EnableQuotaChecking = false };
        var service = CreateService(redisMock.Object, options);

        // Act
        var result = await service.CheckAndConsumeReqSecAsync(1, FreeTier);

        // Assert: request allowed because quota check is disabled
        result.Allowed.Should().BeTrue();
        result.IsQuotaExceeded.Should().BeFalse();
    }

    private static void SetupDbForUnderLimit(Mock<IDatabase> db)
    {
        db.Setup(d => d.SortedSetRemoveRangeByScoreAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        db.Setup(d => d.SortedSetLengthAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>())).ReturnsAsync(RedisValue.Null);
        db.Setup(d => d.SortedSetAddAsync(It.IsAny<RedisKey>(), It.IsAny<RedisValue>(), It.IsAny<double>(), It.IsAny<SortedSetWhen>(), It.IsAny<CommandFlags>())).ReturnsAsync(true);
        db.Setup(d => d.KeyExpireAsync(It.IsAny<RedisKey>(), It.IsAny<TimeSpan?>(), It.IsAny<ExpireWhen>(), It.IsAny<CommandFlags>())).Returns(Task.FromResult(true));
    }

    private static void SetupDbForAtReqSecLimit(Mock<IDatabase> db)
    {
        db.Setup(d => d.SortedSetRemoveRangeByScoreAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        db.Setup(d => d.SortedSetLengthAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(2);
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>())).ReturnsAsync(RedisValue.Null);
    }

    private static void SetupDbForQuotaExceeded(Mock<IDatabase> db)
    {
        db.Setup(d => d.SortedSetRemoveRangeByScoreAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        db.Setup(d => d.SortedSetLengthAsync(It.IsAny<RedisKey>(), It.IsAny<double>(), It.IsAny<double>(), It.IsAny<Exclude>(), It.IsAny<CommandFlags>())).ReturnsAsync(0);
        db.Setup(d => d.StringGetAsync(It.IsAny<RedisKey>())).ReturnsAsync((RedisValue)1000);
    }
}
