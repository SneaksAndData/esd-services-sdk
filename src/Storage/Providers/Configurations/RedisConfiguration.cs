using Snd.Sdk.Hosting;

namespace Snd.Sdk.Storage.Providers.Configurations;

public class RedisConfiguration
{
    public string Host { get; set; }
    public int Port { get; set; }
    public int DatabaseNumber { get; set; }
    public string Password => EnvironmentExtensions.GetDomainEnvironmentVariable("CACHE_REDIS_PASSWORD");
    public bool UseSsl { get; set; }
}
