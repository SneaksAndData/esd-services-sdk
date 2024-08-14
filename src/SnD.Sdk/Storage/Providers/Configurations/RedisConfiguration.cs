using SnD.Sdk.Extensions.Environment.Hosting;


namespace Snd.Sdk.Storage.Providers.Configurations;
/// <summary>
/// Configuration settings for connecting to a Redis instance.
/// </summary>
public class RedisConfiguration
{
    /// <summary>
    ///  Redis server host.
    /// </summary>
    public string Host { get; set; }

    /// <summary>
    ///  Redis server port.
    /// </summary>
    public int Port { get; set; }

    /// <summary>
    /// Redis database number.
    /// </summary>
    public int DatabaseNumber { get; set; }

    /// <summary>
    /// Redis server connection password.
    /// </summary>
    public string Password => EnvironmentExtensions.GetDomainEnvironmentVariable("REDIS_CACHE_PASSWORD");

    /// <summary>
    /// Enable SSL connectivity.
    /// </summary>
    public bool UseSsl { get; set; }
}
