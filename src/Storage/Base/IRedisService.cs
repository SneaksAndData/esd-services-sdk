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
public interface IRedisService
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

    /// <summary>
    /// Retrieves the length of a Redis list.
    /// </summary>
     /// <param name="key">The key of the list from which to remove elements.</param>
    /// <param name="flags">The flags to use for this operation</param>
    /// <returns>Number of elements in the list</returns>
    Task<long> ListLengthAsync(string key, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Removes the first count occurrences of elements equal to value from the list stored at key.
    /// </summary>
    /// <param name="key">The key of the list from which to remove elements.</param>
    /// <param name="value">The element to remove from the list.</param>
    /// <param name="count">The number of occurrences to remove. This parameter can have different effects based on its value:
    /// Positive: Remove up to the specified number of occurrences from the head (left) of the list.
    ///  Negative: Remove up to the specified number of occurrences from the tail (right) of the list.
    ///  Zero: Remove all occurrences of the element.</param>
    /// <param name="flags">The flags to use for this operation</param>
    /// <returns>Number of elements removed</returns>
    Task<long> ListRemoveAsync(string key, RedisValue value, long count = 0L, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Removes and returns the first element of the list stored at key.
    /// </summary>
    /// <param name="key">The key of the list from which to remove elements.</param>
    /// <param name="flags">The flags to use for this operation</param>
    /// <returns>The value</returns>
    Task<RedisValue> ListLeftPopAsync(string key, CommandFlags flags = CommandFlags.None);

    /// <summary>
    ///  Returns the element at index in the list stored at key.
    /// </summary>
    /// <param name="key">The key of the list from which to remove elements.</param>
    /// <param name="index"> The index position to get the value at. The index is zero-based, so 0 means the first element, 1 the second element and so on. Negative indices can be used to designate elements starting at the tail of the list.</param>
    /// <param name="flags">The flags to use for this operation</param>
    /// <returns>The value</returns>
    Task<RedisValue> ListGetByIndexAsync(string key, long index, CommandFlags flags = CommandFlags.None);
/// <summary>
/// Returns the specified elements of the list stored at key.
/// </summary>
/// <param name="key"></param>
/// <param name="start"></param>
/// <param name="stop"></param>
/// <param name="flags"></param>
/// <returns></returns>
    Task<RedisValue[]> ListRangeAsync(string key, int start, int stop, CommandFlags flags = CommandFlags.None);

    /// <summary>
    /// Trim an existing list so that it will contain only the specified range of elements specified
    /// </summary>
    /// <param name="key"></param>
    /// <param name="start"></param>
    /// <param name="stop"></param>
    /// <param name="flags"></param>
    /// <returns></returns>
    Task ListTrimAsync(string key, int start, int stop, CommandFlags flags = CommandFlags.None);
}
