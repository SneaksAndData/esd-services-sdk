using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Models;
/// <summary>
/// Represents a response from sending a message in Amazon SQS.
/// </summary>
public sealed class AmazonSqsSendResponse : IMessageSendResponse
{
    /// <summary>
    /// Gets or sets the ID of the message.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Gets or sets the sequence number of the message.
    /// </summary>
    public string SequenceNumber { get; set; }
}
