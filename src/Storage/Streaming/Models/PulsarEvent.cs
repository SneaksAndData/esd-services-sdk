using System;
using System.Text.Json;
using Avro;
using Pulsar.Client.Common;
using Snd.Sdk.Storage.Streaming.MessageProtocolExtensions;

namespace Snd.Sdk.Storage.Streaming.Models;

/// <summary>
/// A generic record to capture data from a Pulsar message
/// </summary>
/// <typeparam name="TKey">Type to deserialize key to</typeparam>
/// <typeparam name="TData">Type to deserialize data to</typeparam>
public record PulsarEvent<TKey, TData>
{
    private PulsarEvent()
    {
    }

    /// <summary>
    /// Parses a Pulsar message into a PulsarDataCapture object for further processing using supplied AVRO schemas
    /// </summary>
    /// <param name="message">Pulsar Message</param>
    /// <param name="keySchema">AVRO Key Schema</param>
    /// <param name="valueSchema">AVRO Data Schema</param>
    /// <returns></returns>
    public static PulsarEvent<TKey, TData> FromPulsarMessage(Message<byte[]> message, Schema keySchema,
        Schema valueSchema)
    {
        return new PulsarEvent<TKey, TData>
        {
            MessageId = message.MessageId,
            MessagePublishTime = message.PublishTime,
            Key = JsonSerializer.Deserialize<TKey>(AvroExtensions.AvroToJson(Convert.FromBase64String(message.Key),
                keySchema, true)),
            Data = JsonSerializer.Deserialize<TData>(message.Data.Length > 0
                ? AvroExtensions.AvroToJson(message.Data, valueSchema, true)
                : "{}")
        };
    }

    /// <summary>
    /// Creates a PulsarEvent with empty values
    /// </summary>
    public static PulsarEvent<TKey, TData> Empty { get; } = new()
    { Data = default, Key = default, MessagePublishTime = long.MinValue, MessageId = null };

    /// <summary>
    /// Pulsar message id
    /// </summary>
    public MessageId MessageId { get; init; }

    /// <summary>
    /// The time the message was published in unix timestamp format
    /// </summary>
    public long MessagePublishTime { get; init; }

    /// <summary>
    /// Deserialized "Key" part of the message
    /// </summary>
    public TKey Key { get; init; }

    /// <summary>
    /// Deserialized "Data" part of the message
    /// </summary>
    public TData Data { get; init; }
}
