using Akka;
using Akka.Streams.Dsl;
using System;
using System.Threading;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base
{
    /// <summary>
    /// Remote object queue abstractions
    /// </summary>
    public interface IQueueService<TSendResponse, TReleaseResponse>
    {
        /// <summary>
        /// Retrieves from a queue one-at-a-time, popping prefetchCount at a first iteration.
        /// </summary>
        /// <param name="queueName">Name of a queue.</param> 
        /// <param name="visibilityTimeout">Time before a message becomes available to consumers again</param>
        /// <param name="prefetchCount">Number of messages to prefetch.</param>
        /// <param name="pollInterval">Interval to poll empty queue by.</param>
        /// <returns></returns>
        Source<QueueElement, NotUsed> GetQueueMessages(string queueName, TimeSpan visibilityTimeout, int prefetchCount, TimeSpan pollInterval);

        /// <summary>
        /// Sends message to a queue.
        /// </summary>
        /// <param name="queueName">Name of a queue.</param> 
        /// <param name="messageText">Message content</param>
        /// <returns></returns>
        Task<TSendResponse> SendQueueMessage(string queueName, string messageText, CancellationToken cancellationToken = default);

        /// <summary>
        /// Removes a message from a queue.
        /// </summary>
        /// <param name="queueName">Name of a queue.</param> 
        /// <param name="receiptId">Pop receipt for the message</param>
        /// <param name="messageId">Identifier of the message</param>
        /// <returns>Result of the operation as boolean.</returns>
        Task<bool> RemoveQueueMessage(string queueName, string receiptId, string messageId, CancellationToken cancelaltionToken = default);

        /// <summary>
        /// Updates message visibility timeout to 0, unhiding it from consumers.
        /// </summary>
        /// <param name="queueName">Name of a queue.</param> 
        /// <param name="receiptId">Pop receipt for the message</param>
        /// <param name="messageId">Identifier of the message</param>
        /// <returns></returns>
        Task<TReleaseResponse> ReleaseMessage(string queueName, string receiptId, string messageId, CancellationToken cancellationToken = default);

    }
}
