using System;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Metrics.Base;
using Snd.Sdk.Metrics.Configurations;

namespace Snd.Sdk.Metrics.Providers
{
    /// <summary>
    /// Add Datadog implementation of a Metrics Service to the DI containers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DatadogServiceProvider
    {
        /// <summary>
        /// Inject Datadog telemetry client.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="metricNamespace">The root namespace for application metrics (usually application name).</param>
        /// <param name="configureAction">Configuration override callback.
        /// The base configuration is being created using <see cref="DatadogConfiguration.Default"/> method.</param>
        /// <returns>Configured service collection with injected MetricsService</returns>
        public static IServiceCollection AddDatadogMetrics(this IServiceCollection services,
            string metricNamespace,
            Action<DatadogConfiguration> configureAction = null)
        {
            var configuration = DatadogConfiguration.Default(metricNamespace);
            configureAction?.Invoke(configuration);
            return services.AddDatadogMetrics(configuration);
        }

        /// <summary>
        /// Inject Datadog telemetry client.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Base configuration to be provided for MetricsService</param>
        /// <param name="configureAction">Configuration override callback.</param>
        /// <returns>Configured service collection with injected MetricsService</returns>
        public static IServiceCollection AddDatadogMetrics(this IServiceCollection services,
            DatadogConfiguration configuration,
            Action<DatadogConfiguration> configureAction = null)
        {
            configureAction?.Invoke(configuration);
            return services.AddSingleton(typeof(MetricsService), _ => new DatadogMetricsService(configuration));
        }
    }
}
