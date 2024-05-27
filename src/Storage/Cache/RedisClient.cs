using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Base;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

/// <summary>
/// Redis client for performing operations on Redis.
/// </summary>
[ExcludeFromCodeCoverage]
public class RedisClient : IRedisClient
{
    private readonly ConnectionMultiplexer redis;

    /// <summary>
    /// Initializes a new instance of the RedisClient class.
    /// </summary>
    public RedisClient(string host, int databaseNumber, int port = 6380, bool ssl = true)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { host, port } },
            Password = Environment.GetEnvironmentVariable("PROTEUS__CACHE_REDIS_PASSWORD"),
            Ssl = ssl,
            DefaultDatabase = databaseNumber
        };

        redis = ConnectionMultiplexer.Connect(options);
    }

    /// <inheritdoc />
    public Task<bool> MultiExistsAsync(HashSet<string> keys)
    {
        var db = redis.GetDatabase();
        var tasks = keys.Select(key => db.KeyExistsAsync(key)).ToArray();
        return Task.WhenAll(tasks).ContinueWith(task => task.Result.All(exists => exists));
    }

    /// <inheritdoc />
    public Task EvictAsync(string key)
    {
        var db = redis.GetDatabase();
        return db.KeyDeleteAsync(key);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key)
    {
        var db = redis.GetDatabase();
        return db.KeyExistsAsync((key));
    }

    public Task<RedisValue> GetAsync(string key)
    {
        var db = redis.GetDatabase();
        return db.StringGetAsync(key);
    }

    /// <inheritdoc />
    public Task<RedisValue[]> MultiGetAsync(List<string> keys)
    {
        var db = redis.GetDatabase();
        var redisKeys = keys.ConvertAll(k => (RedisKey)k).ToArray();
        return db.StringGetAsync(redisKeys);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan expiresAfter)
    {
        var db = redis.GetDatabase();
        return db.StringSetAsync(key, value, expiresAfter);
    }

    /// <inheritdoc />
    public Task SetExpirationAsync(string key, TimeSpan expiresAfter)
    {
        var db = redis.GetDatabase();
        return db.KeyExpireAsync(key, expiresAfter);
    }

    /// <summary>
    /// Gets the ConnectionMultiplexer instance used by this RedisClient.
    /// </summary>
    /// <returns>The ConnectionMultiplexer instance.</returns>
    public ConnectionMultiplexer GetConnectionMultiplexer()
    {
        return redis;
    }
}
