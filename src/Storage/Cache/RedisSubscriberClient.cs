using System;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Storage.Base;
using StackExchange.Redis;

namespace SnD.Sdk.Storage.Cache;
[ExcludeFromCodeCoverage]
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
        subscriber.SubscribeAsync(channel, handler);
    }

    /// <inheritdoc />
    public void Unsubscribe(string channel)
    {
        subscriber.UnsubscribeAsync(channel);
    }

}
