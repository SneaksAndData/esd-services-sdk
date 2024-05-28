using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using StackExchange.Redis;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Interface for Redis client operations.
/// </summary>
public interface IRedisClient
{
    /// <summary>
    /// Gets the Redis database instance.
    /// </summary>
    IDatabase GetDatabase();

    /// <summary>
    /// Removes a key from Redis.
    /// </summary>
    Task EvictAsync(string key);

    /// <summary>
    /// Checks if a key exists in Redis.
    /// </summary>
    Task<bool> ExistsAsync(string key);

    /// <summary>
    /// Retrieves a value from Redis by key.
    /// </summary>
    Task<RedisValue> GetAsync(string key);

    /// <summary>
    /// Retrieves multiple values from Redis by their keys.
    /// </summary>
    Source<RedisValue, NotUsed> MultiGetAsync(List<string> keys);

    /// <summary>
    /// Sets a key-value pair in Redis with an expiration time.
    /// </summary>
    Task SetAsync(string key, string value, TimeSpan expiresAfter);

    /// <summary>
    /// Sets an expiration time for a key in Redis.
    /// </summary>
    Task SetExpirationAsync(string key, TimeSpan expiresAfter);
}
