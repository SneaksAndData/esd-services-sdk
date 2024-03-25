using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snd.Sdk.Storage.Streaming.Models;

/// <summary>  
/// Represents a message received through an Apache Pulsar Websocket connection.   
/// Includes information such as the message ID, payload, properties, publish time, redelivery count, and key.  
/// </summary>  
public record PulsarWebsocketMessage
{
    /// <summary>  
    /// Gets or sets the identifier of the message.  
    /// </summary>  
    [JsonPropertyName("messageId")]
    public string MessageId { get; set; }

    /// <summary>  
    /// Gets or sets the payload of the message.  
    /// </summary>  
    [JsonPropertyName("payload")]
    public string Payload { get; set; }

    /// <summary>  
    /// Gets or sets the properties of the message.  
    /// </summary>  
    [JsonPropertyName("properties")]
    public Dictionary<string, string> Properties { get; set; }

    /// <summary>  
    /// Gets or sets the time when the message was published.  
    /// </summary>  
    [JsonPropertyName("publishTime")]
    public DateTime PublishTime { get; set; }

    /// <summary>  
    /// Gets or sets the count of how many times the message has been redelivered.  
    /// </summary>  
    [JsonPropertyName("redeliveryCount")]
    public int RedeliveryCount { get; set; }

    /// <summary>  
    /// Gets or sets the key of the message.  
    /// </summary>  
    [JsonPropertyName("key")]
    public string Key { get; set; }
}
