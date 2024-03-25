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
    public static class AzureMonitorServiceProvider
    {
        /// <summary>
        /// Inject App Insights telemetry client and AzMon Metric Service.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureMonitor(this IServiceCollection services)
        {
            return services.AddApplicationInsightsTelemetry(conf =>
            {
                var defaults = AzureMonitorConfiguration.Default;
                conf.ConnectionString = defaults.ConnectionString;
                conf.EnableQuickPulseMetricStream = defaults.EnableQuickPulseMetricStream;
                conf.EnableDebugLogger = defaults.EnableDebugLogger;
                conf.EnableAzureInstanceMetadataTelemetryModule = defaults.EnableAzureInstanceMetadataTelemetryModule;
                conf.EnableAppServicesHeartbeatTelemetryModule = defaults.EnableAppServicesHeartbeatTelemetryModule;
                conf.EnableAuthenticationTrackingJavaScript = defaults.EnableAuthenticationTrackingJavaScript;
                conf.EnablePerformanceCounterCollectionModule = defaults.EnablePerformanceCounterCollectionModule;
                conf.ApplicationVersion = defaults.ApplicationVersion;
                conf.DeveloperMode = defaults.DeveloperMode;
            }).AddSingleton<MetricsService, AzureMonitorService>();
        }
    }
}
