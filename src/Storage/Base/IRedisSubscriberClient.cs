using System;
using StackExchange.Redis;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Interface for Redis subscriber operations.
/// </summary>
public interface IRedisSubscriberClient
{
    /// <summary>
    /// Subscribes to a channel and handles messages that are published to that channel.
    /// </summary>
    /// <param name="channel">The channel to subscribe to.</param>
    /// <param name="handler">The handler function to call when a message is published to the channel.</param>
    void Subscribe(string channel, Action<RedisChannel, RedisValue> handler);

    /// <summary>
    /// Unsubscribes from a channel.
    /// </summary>
    /// <param name="channel">The channel to unsubscribe from.</param>
    void Unsubscribe(string channel);

}
