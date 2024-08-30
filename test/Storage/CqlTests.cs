using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Cql;
using Xunit;

namespace Snd.Sdk.Tests.Storage
{
    public class CqlTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;

        public CqlTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
        }

        [Theory]
        [InlineData(1000, 1, true)]
        [InlineData(50000, 1, true)]
        public async Task ExecuteWithRetryAndRateLimit_ExecutesSuccessfully(int rateLimit, int rateLimitPeriodSeconds, bool expectedResult)
        {
            var loggerMock = new Mock<ILogger<object>>();
            var cqlApiCallMock = new Mock<Func<CancellationToken, Task<bool>>>();
            cqlApiCallMock.Setup(c => c(It.IsAny<CancellationToken>())).ReturnsAsync(expectedResult);
            var cancellationToken = CancellationToken.None;

            var result = await cqlApiCallMock.Object.ExecuteWithRetryAndRateLimit(loggerMock.Object, rateLimit, TimeSpan.FromSeconds(rateLimitPeriodSeconds), cancellationToken);

            Assert.True(result);
        }
    }
}
