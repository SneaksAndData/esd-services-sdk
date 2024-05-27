using System;
using System.Collections.Generic;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Interface for Redis client operations.
/// </summary>
public interface IRedisClient
{
    /// <summary>
    /// Checks if all keys exist in Redis.
    /// </summary>
    bool MultiExists(HashSet<string> keys);

    /// <summary>
    /// Removes a key from Redis.
    /// </summary>
    void Evict(string key);

    /// <summary>
    /// Checks if a key exists in Redis.
    /// </summary>
    bool Exists(string key);

    /// <summary>
    /// Retrieves a value from Redis by key.
    /// </summary>
    string Get(string key);

    /// <summary>
    /// Retrieves multiple values from Redis by their keys.
    /// </summary>
    List<string> MultiGet(List<string> keys);

    /// <summary>
    /// Sets a key-value pair in Redis with an expiration time.
    /// </summary>
    void Set(string key, string value, TimeSpan expiresAfter);

    /// <summary>
    /// Sets an expiration time for a key in Redis.
    /// </summary>
    void SetExpiration(string key, TimeSpan expiresAfter);
}
