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
    Source<StreamEntry, NotUsed> StreamReadAsync(string streamName, string initialId, int count, CommandFlags flags);

}
