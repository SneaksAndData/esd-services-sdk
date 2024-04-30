using System;
using Snd.Sdk.Storage.Base;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;

public class RedisSubscriberClient : IRedisSubscriberClient
{
    private readonly ISubscriber subscriber;

    /// <summary>
    /// Initializes a new instance of the RedisSubscriber class.
    /// </summary>
    public RedisSubscriberClient(ConnectionMultiplexer redis)
    {
        subscriber = redis.GetSubscriber();
    }

    /// <inheritdoc />
    public void Subscribe(string channel, Action<RedisChannel, RedisValue> handler)
    {
        subscriber.Subscribe(channel, handler);
    }

    /// <inheritdoc />
    public void Unsubscribe(string channel)
    {
        subscriber.Unsubscribe(channel);
    }

}
