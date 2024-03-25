using Microsoft.ApplicationInsights.AspNetCore.Extensions;
using System;
using System.Diagnostics.CodeAnalysis;

namespace Snd.Sdk.Metrics.Configurations
{
    /// <summary>
    /// Configuration for Azure Monitor metrics integration.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class AzureMonitorConfiguration
    {
        /// <summary>
        /// Default Azure Monitor config settings.
        /// </summary>
        public static ApplicationInsightsServiceOptions Default => new ApplicationInsightsServiceOptions
        {
            ConnectionString = Environment.GetEnvironmentVariable("APPLICATION_INSIGHTS_CONNECTION_STRING"),
            EnableQuickPulseMetricStream = false,
            EnableDebugLogger = false,
            EnableAzureInstanceMetadataTelemetryModule = false,
            EnableAppServicesHeartbeatTelemetryModule = false,
            EnableAuthenticationTrackingJavaScript = false,
            EnablePerformanceCounterCollectionModule = false,
            ApplicationVersion = Environment.GetEnvironmentVariable("APPLICATION_VERSION"),
            DeveloperMode = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT").ToLowerInvariant() == "development",
        };

        /// <summary>
        /// Series cap for App Insights SDK.
        /// </summary>
        public int SeriesCap { get; set; }

        /// <summary>
        /// Dimensions value cap for App Insights SDK.
        /// </summary>
        public int DimensionsValueCap { get; set; }
    }
}
