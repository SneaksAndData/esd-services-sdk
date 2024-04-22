using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Metrics.Base;
using Snd.Sdk.Metrics.Configurations;

namespace Snd.Sdk.Metrics.Providers
{
    /// <summary>
    /// Add Azure Monitor implementation of a Metrics Service to the DI containers.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class DatadogServiceProvider
    {
        /// <summary>
        /// Inject App Insights telemetry client and AzMon Metric Service.
        /// </summary>
        /// <param name="services">Service collection.</param>
        /// <param name="configuration">Datadog metrics service configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddDatadogMetrics(this IServiceCollection services, DatadogConfiguration configuration)
        {
            return services.AddSingleton(typeof(MetricsService), provider => new DatadogMetricsService(configuration));
        }
    }
}
