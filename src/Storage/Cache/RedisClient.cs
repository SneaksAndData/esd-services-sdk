using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
    public RedisClient(string host, int databaseNumber, int port = 6380)
    {
        var options = new ConfigurationOptions
        {
            EndPoints = { { host, port } },
            Password = Environment.GetEnvironmentVariable("PROTEUS__CACHE_REDIS_PASSWORD"),
            Ssl = true,
            DefaultDatabase = databaseNumber
        };

        redis = ConnectionMultiplexer.Connect(options);
    }

    /// <inheritdoc />
    public bool MultiExists(List<string> keys)
    {
        var db = redis.GetDatabase();
        int existsCount = 0;

        foreach (var key in keys)
        {
            if (db.KeyExists(key))
            {
                existsCount++;
            }
        }

        return existsCount == keys.Count;
    }

    public void Evict(string key)
    {
        var db = redis.GetDatabase();
        db.KeyDelete(key);
    }

    /// <inheritdoc />
    public bool Exists(string key)
    {
        var db = redis.GetDatabase();
        return db.KeyExists(key);
    }

    public string Get(string key)
    {
        var db = redis.GetDatabase();
        return db.StringGet(key);
    }

    /// <inheritdoc />
    public List<string> MultiGet(List<string> keys)
    {
        var db = redis.GetDatabase();
        var redisKeys = keys.ConvertAll(k => (RedisKey)k).ToArray();
        var redisValues = db.StringGet(redisKeys);
        var results = new List<string>();

        foreach (var value in redisValues)
        {
            results.Add(value.ToString());
        }

        return results;
    }

    /// <inheritdoc />
    public void Set(string key, string value, TimeSpan expiresAfter)
    {
        var db = redis.GetDatabase();
        db.StringSet(key, value, expiresAfter);
    }

    /// <inheritdoc />
    public void SetExpiration(string key, TimeSpan expiresAfter)
    {
        var db = redis.GetDatabase();
        db.KeyExpire(key, expiresAfter);
    }
}
