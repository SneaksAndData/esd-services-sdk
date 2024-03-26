using Serilog;
using Serilog.Sinks.Datadog.Logs;
using System;
using System.Diagnostics.CodeAnalysis;
using System.Net;

namespace Snd.Sdk.Logs.Providers.Configurations
{
    /// <summary>
    /// Methods to load Datadog configuration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DatadogLoggingConfiguration
    {
        private const string proteusDatadogApiKey = "PROTEUS__DATADOG_API_KEY";
        private const string proteusDatadogSite = "PROTEUS__DATADOG_SITE";

        private static string GetApiKey()
        {
            return Environment.GetEnvironmentVariable(proteusDatadogApiKey);
        }

        private static DatadogConfiguration CreateDefault()
        {
            return new DatadogConfiguration
            {
                Url = Environment.GetEnvironmentVariable(proteusDatadogSite)
            };
        }

        private static bool IsDatadogEnabled()
        {
            return !string.IsNullOrEmpty(Environment.GetEnvironmentVariable(proteusDatadogApiKey));
        }

        /// <summary>
        /// Adds datadog provider to serilog logging configuration
        /// </summary>
        /// <param name="baseConfiguration">Configuration of logger</param>
        /// <returns></returns>
        public static LoggerConfiguration AddDatadog(this LoggerConfiguration baseConfiguration)
        {
            if (IsDatadogEnabled())
            {
                return baseConfiguration.WriteTo.DatadogLogs(
                    host: Dns.GetHostName(),
                    apiKey: GetApiKey(),
                    configuration: CreateDefault());
            }
            return baseConfiguration;
        }
    }
}
