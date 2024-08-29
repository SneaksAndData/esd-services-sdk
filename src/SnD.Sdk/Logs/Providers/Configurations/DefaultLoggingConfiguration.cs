using System.Diagnostics.CodeAnalysis;
using Serilog;

namespace Snd.Sdk.Logs.Providers.Configurations
{
    /// <summary>
    /// Extension methods for configuration of all sinks
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DefaultLoggingConfiguration
    {
        /// <summary>
        /// Crates a default logging configuration
        /// </summary>
        /// <param name="loggerConfiguration">Serilog's configuration class</param>
        /// <returns></returns>
        public static LoggerConfiguration Default(this LoggerConfiguration loggerConfiguration)
        {
            return loggerConfiguration.WriteTo.Console();
        }
    }
}
