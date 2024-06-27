using System;
using System.Diagnostics.CodeAnalysis;
using Newtonsoft.Json;
using Snd.Sdk.Hosting;

namespace Snd.Sdk.Storage.Providers.Configurations;

/// <summary>
/// Configuration for Cql API-compatible Storage.
/// </summary>
[ExcludeFromCodeCoverage]
public class CqlConfiguration
{
    /// <summary>
    /// Node addresses used to connect to the cluster.
    /// </summary>
    public string[] ContactPoints { get; set; }

    /// <summary>
    /// Name of a key space to connect to.
    /// </summary>
    public string KeySpace { get; set; }

    /// <summary>
    /// Optional datacenter name for network topology settings and load balancing.
    /// </summary>
    public string DataCenter { get; set; }

    /// <summary>
    /// Connection user name.
    /// </summary>
    [JsonIgnore]
    public string Username => EnvironmentExtensions.GetDomainEnvironmentVariable("CQL_USER");

    /// <summary>
    /// Connection password.
    /// </summary>
    [JsonIgnore]
    public string Password => EnvironmentExtensions.GetDomainEnvironmentVariable("CQL_PASSWORD");

    /// <summary>
    /// Name for the connecting application.
    /// </summary>
    [JsonIgnore]
    public string ApplicationName => EnvironmentExtensions.GetDomainEnvironmentVariable("APPLICATION_NAME");

    /// <summary>
    /// Base wait time for exponential retries for reconnect attempts, in ms.
    /// </summary>
    public long ReconnectBaseDelayMs { get; set; }

    /// <summary>
    /// Max wait time for exponential retries for reconnect attempts, in ms.
    /// </summary>
    public long ReconnectMaxDelayMs { get; set; }

    /// <summary>
    /// Query timeout in ms.
    /// </summary>
    public int QueryTimeout { get; set; }

    /// <summary>
    /// Socket connect timeout in ms.
    /// </summary>
    public int SocketConnectionTimeout { get; set; }

    /// <summary>
    /// Socket read timeout in ms.
    /// </summary>
    public int SocketReadTimeout { get; set; }

    /// <summary>
    /// Enable SSL transport for the driver.
    /// </summary>
    public bool UseSsl { get; set; }
}
