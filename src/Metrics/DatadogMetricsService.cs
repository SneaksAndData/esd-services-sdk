using StatsdClient;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Snd.Sdk.Metrics.Base;
using Snd.Sdk.Metrics.Configurations;

namespace Snd.Sdk.Metrics
{
    /// <summary>
    /// Implementation of MetricsService for Datadog.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public class DatadogMetricsService : MetricsService, IDisposable
    {
        private DogStatsdService dogStatsdService;

        /// <summary>
        /// Constructs a new instance of <see cref="DatadogMetricsService"/>.
        /// </summary>
        /// <param name="datadogConfiguration"></param>
        public DatadogMetricsService(DatadogConfiguration datadogConfiguration)
        {
            dogStatsdService = new DogStatsdService();
            dogStatsdService.Configure(datadogConfiguration.StatsdConfig);
        }

        private string[] ConvertTags(IDictionary<string, string> tagsDict)
        {
            return tagsDict.Select(kv => $"{kv.Key}:{kv.Value}").ToArray();
        }

        /// <inheritdoc />
        public override void Count(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Counter(metricName, metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Decrement(string metricName, SortedDictionary<string, string> tags, int metricValue = 1)
        {
            this.dogStatsdService.Decrement(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <summary>
        /// Disposes a metrics service and flushes all metrics.
        /// </summary>
        public void Dispose()
        {
            this.dogStatsdService.Dispose();
        }

        /// <inheritdoc />
        public override void Gauge(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Gauge(metricName, value: (double)metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Gauge(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Gauge(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Histogram(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Histogram(metricName, value: (double)metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Histogram(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Histogram(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Increment(string metricName, SortedDictionary<string, string> tags, int metricValue = 1)
        {
            this.dogStatsdService.Increment(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Set(string metricName, int metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Set(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Set(string metricName, decimal metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Set(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void Set(string metricName, string metricValue, SortedDictionary<string, string> tags)
        {
            this.dogStatsdService.Set(metricName, value: metricValue, tags: this.ConvertTags(tags));
        }

        /// <inheritdoc />
        public override void HealthCheck(string checkName, int checkValue, SortedDictionary<string, string> tags)
        {
            var status = checkValue switch
            {
                0 => Status.WARNING,
                1 => Status.OK,
                -1 => Status.CRITICAL,
                _ => Status.UNKNOWN
            };

            this.dogStatsdService.ServiceCheck(name: checkName, status: status, tags: this.ConvertTags(tags));
        }
    }
}
