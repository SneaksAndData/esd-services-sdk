namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Interface for Redis publisher operations.
/// </summary>
public interface IRedisPublisherClient
{
    /// <summary>
    /// Publishes a message to a channel in Redis.
    /// </summary>
    /// <param name="channel">The channel to publish the message to.</param>
    /// <param name="message">The message to publish.</param>
    void Publish(string channel, string message);

}
