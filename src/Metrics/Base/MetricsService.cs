using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;

namespace Snd.Sdk.Metrics.Base
{
    /// <summary>
    /// Service for reporting metrics to external providers.
    /// Metric types are based on StatsD terminology.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public abstract class MetricsService
    {
        /// <summary>
        /// GAUGE metric (last value over flush interval).
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Gauge(string metricName, decimal metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// GAUGE metric (last value over flush interval).
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Gauge(string metricName, int metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Increment a COUNT metric and submit the result.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value to increment by.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Increment(string metricName, SortedDictionary<string, string> tags, int metricValue = 1);

        /// <summary>
        /// Decrement a COUNT metric and submit the result.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value to decrement by.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Decrement(string metricName, SortedDictionary<string, string> tags, int metricValue = 1);

        /// <summary>
        /// Submit a COUNT metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Count(string metricName, int metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Submit a SET metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Set(string metricName, int metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Submit a SET metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Set(string metricName, decimal metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Submit a SET metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Set(string metricName, string metricValue, SortedDictionary<string, string> tags);
        /// <summary>
        /// Submit a HISTOGRAM metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Histogram(string metricName, decimal metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Submit a HISTOGRAM metric.
        /// </summary>
        /// <param name="metricName">Name of a metric.</param>
        /// <param name="metricValue">Value of a metric.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void Histogram(string metricName, int metricValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Submit a health check metric.
        /// </summary>
        /// <param name="checkName">Name of a service a health check is submitted for.</param>
        /// <param name="checkValue">Check status: 1 - OK, 0 - Warning, -1 - Error. Other values should be treated as Undefined/Unknown.</param>
        /// <param name="tags">Tags assigned to this metric.</param>
        public abstract void HealthCheck(string checkName, int checkValue, SortedDictionary<string, string> tags);

        /// <summary>
        /// Generates a human-friendly metric name.
        /// </summary>
        /// <param name="metricNamespace">Namespace of a metric.</param>
        /// <param name="metricName">Name of a metric.</param>
        /// <returns></returns>
        public string GetMetricName(string metricNamespace, string metricName) => $"{metricNamespace}.{metricName}";
    }
}
