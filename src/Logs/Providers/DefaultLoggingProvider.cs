using Microsoft.Extensions.Hosting;
using Serilog;
using Serilog.Events;
using System;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Hosting;

namespace Snd.Sdk.Logs.Providers
{
    /// <summary>
    /// Add Datadog implementation of a Logging Service to the DI containers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DefaultLoggingProvider
    {
        /// <summary>
        /// Creates serilog logger for asp host builder
        /// </summary>
        /// <param name="builder">ASP.Net core host builder</param>
        /// <param name="applicationName">name of application, e.g. nameof(Crystal)</param>
        /// <param name="configureLogger">Delegate that changes logger configuration options</param>
        /// <returns></returns>
        public static IHostBuilder AddSerilogLogger(this IHostBuilder builder, string applicationName,
            Func<LoggerConfiguration, LoggerConfiguration> configureLogger)
        {
            return builder.AddSerilogLogger(applicationName, (_, services, loggerConfiguration) =>
            {
                configureLogger?.Invoke(loggerConfiguration.BaseConfiguration(services, applicationName));
            });
        }

        /// <summary>
        /// Creates serilog logger for asp host builder
        /// </summary>
        /// <param name="builder">ASP.Net core host builder</param>
        /// <param name="applicationName">name of application, e.g. nameof(Crystal)</param>
        /// <param name="configureLogger">Delegate that changes logger configuration options</param>
        /// <returns></returns>
        public static IHostBuilder AddSerilogLogger(this IHostBuilder builder,
            string applicationName,
            Action<HostBuilderContext, IServiceProvider, LoggerConfiguration> configureLogger = null)
        {
            return builder.UseSerilog((hostingContext, services, loggerConfiguration) =>
            {
                var configuration = loggerConfiguration.BaseConfiguration(services, applicationName);
                configureLogger?.Invoke(hostingContext, services, configuration);
            });
        }

        /// <summary>
        /// Creates serilog logger for asp host builder
        /// </summary>
        /// <param name="applicationName">name of application, e.g. nameof(ConsoleApplication)</param>
        /// <param name="configure">Optional configuration override callback</param>
        /// <returns></returns>
        public static ILogger CreateBootstrapLogger(string applicationName,
            Func<LoggerConfiguration, LoggerConfiguration> configure = null)
        {
            var configuration = new LoggerConfiguration()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Information)
                .Enrich.FromLogContext()
                .WriteTo.Console()
                .EnrichWithCommonProperties(applicationName);
            return (configure?.Invoke(configuration) ?? configuration).CreateBootstrapLogger();
        }

        private static LoggerConfiguration EnrichWithCommonProperties(this LoggerConfiguration loggerConfiguration,
            string applicationName)
        {
            return loggerConfiguration.Enrich.WithProperty("Application", applicationName)
                .Enrich.WithProperty("Environment", Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT") ?? "Local")
                .Enrich.WithProperty("ApplicationVersion", Environment.GetEnvironmentVariable("APPLICATION_VERSION") ?? "v0.0.0");
        }

        private static LoggerConfiguration BaseConfiguration(this LoggerConfiguration loggerConfiguration,
            IServiceProvider services, string applicationName)
        {
            return (EnvironmentExtensions.GetDomainEnvironmentVariable("DEFAULT_LOG_LEVEL") switch
            {
                "INFO" => loggerConfiguration.MinimumLevel.Information(),
                "WARN" => loggerConfiguration.MinimumLevel.Warning(),
                "ERROR" => loggerConfiguration.MinimumLevel.Error(),
                "DEBUG" => loggerConfiguration.MinimumLevel.Debug(),
                _ => loggerConfiguration.MinimumLevel.Information()
            }).ReadFrom.Services(services)
                .Enrich.FromLogContext()
                .EnrichWithCommonProperties(applicationName);
        }
    }
}
