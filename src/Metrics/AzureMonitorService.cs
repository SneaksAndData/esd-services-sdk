using Microsoft.ApplicationInsights;
using Microsoft.ApplicationInsights.Metrics;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading;
using Snd.Sdk.Metrics.Base;
using Snd.Sdk.Metrics.Configurations;

namespace Snd.Sdk.Metrics
{
    /// <summary>
    /// Metrics implementation for Azure Monitor.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class AzureMonitorService : MetricsService
    {
        private readonly TelemetryClient telemetryClient;
        private readonly ILogger<AzureMonitorService> logger;
        private readonly Dictionary<string, MetricIdentifier> registeredMetrics;
        private readonly MetricConfiguration metricConfig;
        private const int MICROSOFT_SLEEP_FACTOR = 5000;

        /// <summary>
        /// Constructs a new instance of <see cref="AzureMonitorService"/>
        /// </summary>
        /// <param name="telemetryClient">AspNetCore Telemetry Client.</param>
        /// <param name="logger">Logger instance for this class.</param>
        /// <param name="options">Configuration options for Azmon.</param>
        public AzureMonitorService(TelemetryClient telemetryClient, ILogger<AzureMonitorService> logger, IOptions<AzureMonitorConfiguration> options) : base()
        {
            this.telemetryClient = telemetryClient;
            this.logger = logger;
            this.registeredMetrics = new Dictionary<string, MetricIdentifier>();
            this.metricConfig = new MetricConfiguration(
                seriesCountLimit: options.Value.SeriesCap,
                valuesPerDimensionLimit: options.Value.DimensionsValueCap,
                new MetricSeriesConfigurationForMeasurement(restrictToUInt32Values: false)
            );
        }

        /// <inheritdoc />
        public override void Gauge(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            var dimensionKeys = tags.Keys.ToList();
            var metricNs = metricName.Split('.')[0];
            if (!this.registeredMetrics.TryGetValue(metricName, out var registeredMetricId))
            {
                registeredMetricId = tags.Count switch
                {
                    0 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName),
                    1 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0]),
                    2 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1]),
                    3 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2]),
                    4 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3]),
                    5 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4]),
                    6 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4], dimension6Name: dimensionKeys[5]),
                    7 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4], dimension6Name: dimensionKeys[5], dimension7Name: dimensionKeys[6]),
                    8 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4], dimension6Name: dimensionKeys[5], dimension7Name: dimensionKeys[6], dimension8Name: dimensionKeys[7]),
                    9 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4], dimension6Name: dimensionKeys[5], dimension7Name: dimensionKeys[6], dimension8Name: dimensionKeys[7], dimension9Name: dimensionKeys[8]),
                    10 => new MetricIdentifier(metricNamespace: metricNs, metricId: metricName, dimension1Name: dimensionKeys[0], dimension2Name: dimensionKeys[1], dimension3Name: dimensionKeys[2], dimension4Name: dimensionKeys[3], dimension5Name: dimensionKeys[4], dimension6Name: dimensionKeys[5], dimension7Name: dimensionKeys[6], dimension8Name: dimensionKeys[7], dimension9Name: dimensionKeys[8], dimension10Name: dimensionKeys[9]),
                    _ => throw new ArgumentOutOfRangeException("dimensions", "Can only have up to 10 dimensions in Azure Monitor Metrics")
                };

                this.registeredMetrics.Add(metricName, registeredMetricId);
            }

            var reportedMetric = telemetryClient.GetMetric(registeredMetricId, this.metricConfig);

            var trackResult = tags.Count switch
            {
                0 => new Func<bool>(() => { reportedMetric.TrackValue(metricValue: metricValue); return true; }).Invoke(),
                1 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]]),
                2 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]]),
                3 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]]),
                4 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]]),
                5 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]]),
                6 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]], dimension6Value: tags[dimensionKeys[5]]),
                7 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]], dimension6Value: tags[dimensionKeys[5]], dimension7Value: tags[dimensionKeys[6]]),
                8 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]], dimension6Value: tags[dimensionKeys[5]], dimension7Value: tags[dimensionKeys[6]], dimension8Value: tags[dimensionKeys[7]]),
                9 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]], dimension6Value: tags[dimensionKeys[5]], dimension7Value: tags[dimensionKeys[6]], dimension8Value: tags[dimensionKeys[7]], dimension9Value: tags[dimensionKeys[8]]),
                10 => reportedMetric.TrackValue(metricValue: metricValue, dimension1Value: tags[dimensionKeys[0]], dimension2Value: tags[dimensionKeys[1]], dimension3Value: tags[dimensionKeys[2]], dimension4Value: tags[dimensionKeys[3]], dimension5Value: tags[dimensionKeys[4]], dimension6Value: tags[dimensionKeys[5]], dimension7Value: tags[dimensionKeys[6]], dimension8Value: tags[dimensionKeys[7]], dimension9Value: tags[dimensionKeys[8]], dimension10Value: tags[dimensionKeys[9]]),
                _ => throw new ArgumentOutOfRangeException("dimensions", "Can only have up to 10 dimensions in Azure Monitor Metrics")
            };

            if (!trackResult)
            {
                this.logger.LogWarning("Failed to send {metricName}, series cap reached.", metricName);
            }
        }

        /// <inheritdoc />
        public override void Gauge(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Increment(string metricName, SortedDictionary<string, string> tags, int metricValue = 1)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Decrement(string metricName, SortedDictionary<string, string> tags, int metricValue = 1)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Count(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Set(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Set(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Set(string metricName, string metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Histogram(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void Histogram(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <inheritdoc />
        public override void HealthCheck(string checkName, int checkValue, SortedDictionary<string, string> tags)
        {
            throw new NotImplementedException();
        }

        /// <summary>
        /// https://docs.microsoft.com/en-us/azure/azure-monitor/app/api-custom-events-metrics#flushing-data
        /// </summary>
        ~AzureMonitorService()
        {
            this.telemetryClient.Flush();
            Thread.Sleep(MICROSOFT_SLEEP_FACTOR);
        }
    }
}
