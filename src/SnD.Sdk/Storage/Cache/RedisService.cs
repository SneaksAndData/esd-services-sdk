using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
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
public class RedisService : IRedisService
{

    private readonly IDatabase db;

    /// <summary>
    /// Initializes a new instance of the RedisClient class.
    /// </summary>
    public RedisService(IDatabase db)
    {
        this.db = db;
    }

    /// <inheritdoc />
    public Task EvictAsync(string key)
    {
        return db.KeyDeleteAsync(key);
    }

    /// <inheritdoc />
    public Task<bool> ExistsAsync(string key)
    {
        return db.KeyExistsAsync((key));
    }

    /// <inheritdoc />
    public Task<RedisValue> GetAsync(string key)
    {
        return db.StringGetAsync(key);
    }

    /// <inheritdoc />
    public Source<RedisValue, NotUsed> MultiGetAsync(List<string> keys)
    {
        var redisKeys = keys.ConvertAll(k => (RedisKey)k).ToArray();
        return Source.FromTask(db.StringGetAsync(redisKeys)).SelectMany(k => k);
    }

    /// <inheritdoc />
    public Task SetAsync(string key, string value, TimeSpan expiresAfter)
    {
        return db.StringSetAsync(key, value, expiresAfter);
    }

    /// <inheritdoc />
    public Task SetExpirationAsync(string key, TimeSpan expiresAfter)
    {
        return db.KeyExpireAsync(key, expiresAfter);
    }

    /// <inheritdoc />
    public Task<long> ListLengthAsync(string key, CommandFlags flags = CommandFlags.None)
    {
        return db.ListLengthAsync(key, flags);
    }

    /// <inheritdoc />
    public Task<long> ListRemoveAsync(string key, RedisValue value, long count = 0L,
        CommandFlags flags = CommandFlags.None)
    {
        return db.ListRemoveAsync(key, value, count, flags);
    }

    /// <inheritdoc />
    public Task<RedisValue> ListLeftPopAsync(string key, CommandFlags flags = CommandFlags.None)
    {
        return db.ListLeftPopAsync(key, flags);
    }

    /// <inheritdoc />
    public Task<RedisValue> ListGetByIndexAsync(string key, long index, CommandFlags flags = CommandFlags.None)
    {
        return db.ListGetByIndexAsync(key, index, flags);
    }

    /// <inheritdoc />
    public Task<RedisValue[]> ListRangeAsync(string key, int start, int stop, CommandFlags flags = CommandFlags.None)
    {
        return db.ListRangeAsync(key, start, stop, flags);
    }

    /// <inheritdoc />
    public Task ListTrimAsync(string key, int start, int stop, CommandFlags flags = CommandFlags.None)
    {
        return db.ListTrimAsync(key, start, stop, flags);
    }
}
