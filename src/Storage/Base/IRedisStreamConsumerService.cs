using System;
using Akka;
using Akka.Streams.Dsl;
using StackExchange.Redis;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Interface for Redis subscriber operations.
/// </summary>
public interface IRedisStreamConsumerService
{
    /// <summary>
    /// Asynchronously reads a stream from Redis.
    /// </summary>
    /// <param name="streamName">The name of the stream to read from.</param>
    /// <param name="initialId">The initial ID from which to start reading.</param>
    /// <param name="count">The number of entries to read.</param>
    /// <param name="flags">The command flags to use when reading the stream.</param>
    /// <returns>A source of stream entries.</returns>
    Source<StreamEntry, NotUsed> StreamReadAsync(string streamName, string initialId, int count, CommandFlags flags);

}
