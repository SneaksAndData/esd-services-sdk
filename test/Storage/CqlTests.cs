using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Cql;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class CqlTests: IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
{
    private readonly AkkaFixture akkaFixture;
    private readonly LoggerFixture loggerFixture;
    public CqlTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
    {
        this.akkaFixture = akkaFixture;
        this.loggerFixture = loggerFixture;
    }

    [Theory]
    [InlineData("1000 per second", true)]
    [InlineData("50000 per minute", true)]
    public async Task ExecuteWithRetryAndRateLimit_ExecutesSuccessfully(string rateLimit, bool expectedResult)
    {
        var loggerMock = new Mock<ILogger<object>>();
        var cqlApiCallMock = new Mock<Func<CancellationToken, Task<bool>>>();
        cqlApiCallMock.Setup(c => c(It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
        var cancellationToken = CancellationToken.None;

        var result = await cqlApiCallMock.Object.ExecuteWithRetryAndRateLimit(loggerMock.Object, rateLimit, cancellationToken);

        Assert.True(result);
    }

    [Fact]
    public async Task ExecuteWithRetryAndRateLimit_InvalidRateLimitUnit()
    {
        var loggerMock = new Mock<ILogger<object>>();
        var cqlApiCallMock = new Mock<Func<CancellationToken, Task<int>>>();
        var cancellationToken = CancellationToken.None;

        await Assert.ThrowsAsync<ArgumentException>(() => cqlApiCallMock.Object.ExecuteWithRetryAndRateLimit(loggerMock.Object, "1000 per min", cancellationToken));
    }
}
