using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Snd.Sdk.Storage.Base;
using SnD.Sdk.Storage.Cache;
using Snd.Sdk.Storage.Providers.Configurations;
using StackExchange.Redis;

namespace Snd.Sdk.Storage.Providers;

/// <summary>
/// Provider for Redis service.
/// </summary>
public static class RedisServiceProvider
{
    /// <summary>
    /// Adds Redis connection to the DI container.
    /// </summary>
    /// <param name="services">Service collection (DI container).</param>
    /// <param name="appConfiguration">Application configuration with "RedisServiceProvider" section configured according to <see cref="RedisServiceProvider"/>.</param>
    /// <returns></returns>
    public static IServiceCollection AddRedisCache(this IServiceCollection services, IConfiguration appConfiguration)
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

        services.AddSingleton<IDatabase>(sp => ConnectionMultiplexer.Connect(options).GetDatabase());

        return services.AddSingleton<IRedisService, RedisService>();
    }
}
