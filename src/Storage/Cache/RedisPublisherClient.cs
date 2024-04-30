using Snd.Sdk.Storage.Base;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

public class RedisPublisherClient: IRedisPublisherClient
{

    private readonly ISubscriber publisher;

    public RedisPublisherClient(ConnectionMultiplexer redis)
    {
        publisher = redis.GetSubscriber();
    }

    /// <inheritdoc />
    public void Publish(string channel, string message)
    {
        publisher.Publish(channel, message);
    }
}
