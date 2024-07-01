﻿using StatsdClient;
using System;
using System.Diagnostics.CodeAnalysis;
using SnD.Sdk.Extensions.Environment.Hosting;

namespace Snd.Sdk.Metrics.Configurations
{
    /// <summary>
    /// Configuration for Datadog metrics provider.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class DatadogConfiguration
    {
        /// <summary>
        /// Statsd config generated by the constructor.
        /// </summary>
        public StatsdConfig StatsdConfig { get; private set; }

        /// <summary>
        /// Default constructor, uses UDP connection.
        /// </summary>
        /// <param name="metricNamespace">Namespace for all metrics reported from this app, e.g. application name.</param>
        /// <returns></returns>
        public static DatadogConfiguration Default(string metricNamespace)
        {
            var defaultConf = new StatsdConfig
            {
                StatsdServerName = Environment.GetEnvironmentVariable("PROTEUS__DD_STATSD_HOST") ?? "localhost",
                StatsdPort = int.Parse(Environment.GetEnvironmentVariable("PROTEUS__DD_STATSD_PORT") ?? "8125"),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                ServiceVersion = Environment.GetEnvironmentVariable("APPLICATION_VERSION"),
                Prefix = metricNamespace.ToLower()
            };

            return new DatadogConfiguration { StatsdConfig = defaultConf };
        }

        /// <summary>
        /// Configuration for submitting metrics over UDS.
        /// </summary>
        /// <param name="metricNamespace">Namespace for all metrics reported from this app, e.g. application name.</param>
        /// <returns></returns>
        public static DatadogConfiguration UnixDomainSocket(string metricNamespace)
        {
            var defaultConf = new StatsdConfig
            {
                StatsdServerName = EnvironmentExtensions.GetDomainEnvironmentVariable("DD_UNIX_DOMAIN_SOCKET_PATH", "unix:///var/run/datadog/dsd.socket"),
                Environment = Environment.GetEnvironmentVariable("ASPNETCORE_ENVIRONMENT"),
                ServiceVersion = Environment.GetEnvironmentVariable("APPLICATION_VERSION"),
                Prefix = metricNamespace.ToLower()
            };

            return new DatadogConfiguration { StatsdConfig = defaultConf };
        }
    }
}
