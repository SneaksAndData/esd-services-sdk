using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snd.Sdk.Storage.Base;
using SnD.Sdk.Storage.Cache;
using Snd.Sdk.Storage.Providers.Configurations;
using StackExchange.Redis;

namespace Snd.Sdk.Storage.Providers;

public static class RedisServiceProvider
{
    public static IServiceCollection AddRedisStorage(this IServiceCollection services, IConfiguration appConfiguration)
    {
        var redisConfiguration = new RedisConfiguration();
        appConfiguration.GetSection(nameof(RedisServiceProvider)).Bind(redisConfiguration);

        var options = new ConfigurationOptions()
        {
            EndPoints = { { redisConfiguration.Host, redisConfiguration.Port } },
            Password = redisConfiguration.Password,
            Ssl = redisConfiguration.UseSsl,
            DefaultDatabase = redisConfiguration.DatabaseNumber
        };

        services.AddSingleton(typeof(IConnectionMultiplexer), ConnectionMultiplexer.Connect(options));

        return services.AddSingleton<IRedisClient, RedisClient>();
    }
}
