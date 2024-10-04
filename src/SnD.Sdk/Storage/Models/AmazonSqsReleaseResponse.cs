namespace Snd.Sdk.Storage.Models;

/// <summary>
/// Represents a response from releasing a message in Amazon SQS.
/// </summary>
public sealed class AmazonSqsReleaseResponse
{
    /// <summary>
    /// Gets or sets the ID of the message.
    /// </summary>
    public string MessageId { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the release was successful.
    /// </summary>
    public bool Success { get; set; }
}
