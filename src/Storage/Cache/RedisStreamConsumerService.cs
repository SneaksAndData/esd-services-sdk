using Akka;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Cache.Streaming;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

// [ExcludeFromCodeCoverage]
public class RedisStreamConsumerService : IRedisStreamConsumerService
{
    private readonly IConnectionMultiplexer redis;

    public RedisStreamConsumerService(IConnectionMultiplexer redis)
    {
        this.redis = redis;
    }

    public Source<StreamEntry, NotUsed> StreamReadAsync(string streamName, string initialId, int count, CommandFlags flags)
    {
        return RedisStreamConsumerSource.Create(streamName, redis, initialId, count, flags).Select(e => e);
    }
}
