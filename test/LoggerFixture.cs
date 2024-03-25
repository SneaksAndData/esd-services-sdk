using Microsoft.Extensions.Logging;

namespace Snd.Sdk.Tests
{
    public class LoggerFixture
    {
        public ILoggerFactory Factory { get; }
        public LoggerFixture()
        {
            this.Factory = LoggerFactory.Create(conf => conf.AddConsole());
        }
    }
}
