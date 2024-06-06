using System.Diagnostics.CodeAnalysis;
using Akka;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Cache.Streaming;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

/// <summary>
/// Service for consuming data from Redis streams.
/// </summary>
[ExcludeFromCodeCoverage]
public class RedisStreamConsumerService : IRedisStreamConsumerService
{
    private readonly IConnectionMultiplexer redis;

    /// <summary>
    /// Initializes a new instance of the <see cref="RedisStreamConsumerService"/> class.
    /// </summary>
    /// <param name="redis">The Redis connection multiplexer.</param>
    public RedisStreamConsumerService(IConnectionMultiplexer redis)
    {
        this.redis = redis;
    }

    /// <inheritdoc />
    public Source<StreamEntry, NotUsed> StreamReadAsync(string streamName, string initialId, int count, CommandFlags flags)
    {
        return RedisStreamConsumerSource.Create(streamName, redis, initialId, count, flags).Select(e => e);
    }
}
