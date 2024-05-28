using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Base;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

/// <summary>
/// Redis client for performing operations on Redis.
/// </summary>
[ExcludeFromCodeCoverage]
public class RedisClient : IRedisClient
{
    private readonly IConnectionMultiplexer redis;

    /// <summary>
    /// Initializes a new instance of the RedisClient class.
    /// </summary>
    public RedisClient(IConnectionMultiplexer redis)
    {
        this.redis = redis;
    }

    /// <inheritdoc />
    public IDatabase GetDatabase()
    {
        return redis.GetDatabase();
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

    /// <inheritdoc />
    public Task<RedisValue> GetAsync(string key)
    {
        var db = redis.GetDatabase();
        return db.StringGetAsync(key);
    }

    /// <inheritdoc />
    public Source<RedisValue, NotUsed> MultiGetAsync(List<string> keys)
    {
        var db = GetDatabase();
        var redisKeys = keys.ConvertAll(k => (RedisKey)k).ToArray();
      return Source.FromTask(db.StringGetAsync(redisKeys)).SelectMany(k => k);
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

}
