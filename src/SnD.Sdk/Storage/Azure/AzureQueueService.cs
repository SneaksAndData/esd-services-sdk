using Akka;
using Akka.Streams.Azure.StorageQueue;
using Akka.Streams.Dsl;
using Azure.Storage.Queues;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Azure
{
    /// <summary>
    /// Queue Service implementation for Azure.
    /// </summary>
    public class AzureQueueService : IQueueService
    {
        private readonly QueueServiceClient queueServiceClient;
        private readonly ILogger<AzureQueueService> logger;

        /// <summary>
        /// Creates an instance of <see cref="AzureQueueService"/>.
        /// </summary>
        /// <param name="queueServiceClient"></param>
        /// <param name="logger"></param>
        public AzureQueueService(QueueServiceClient queueServiceClient, ILogger<AzureQueueService> logger)
        {
            this.queueServiceClient = queueServiceClient;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Source<QueueElement, NotUsed> GetQueueMessages(string queueName, TimeSpan visibilityTimeout, int prefetchCount, TimeSpan pollInterval)
        {
            this.logger.LogDebug("Creating a stream from queue: {queueName}, using visibility timeout {visibilityTimeout}", queueName, visibilityTimeout);
            return QueueSource.Create(queue: this.queueServiceClient.GetQueueClient(queueName),
                                      prefetchCount: prefetchCount,
                                      options: new GetRequestOptions(visibilityTimeout),
                                      pollInterval: pollInterval)
                .Select(qm => new QueueElement
                {
                    Content = qm.Body,
                    ElementId = qm.MessageId,
                    DeleteHandle = qm.PopReceipt,
                    DequeueCount = qm.DequeueCount
                });
        }

        /// <inheritdoc />
        public Task<QueueReleaseResponse> ReleaseMessage(string queueName, string receiptId, string messageId)
        {
            this.logger.LogDebug("Changing visibility of {messageId} from {queueName} of account {queueAccount}", messageId, queueName, this.queueServiceClient.AccountName);
            return this.queueServiceClient.GetQueueClient(queueName).UpdateMessageAsync(messageId: messageId, popReceipt: receiptId, visibilityTimeout: TimeSpan.FromSeconds(0))
                .Map(result => new QueueReleaseResponse
                {
                    MessageId = messageId,
                    VisibleAt = result.Value.NextVisibleOn,
                    DeleteHandle = result.Value.PopReceipt
                });
        }

        /// <inheritdoc />
        public Task<bool> RemoveQueueMessage(string queueName, string receiptId, string messageId)
        {
            this.logger.LogDebug("Removing {messageId} from {queueName} of account {queueAccount}", messageId, queueName, this.queueServiceClient.AccountName);
            return this.queueServiceClient.GetQueueClient(queueName).DeleteMessageAsync(messageId, receiptId).Map(result => result.Status == 200);
        }

        /// <inheritdoc />
        public Task<QueueSendResponse> SendQueueMessage(string queueName, string messageText)
        {
            this.logger.LogDebug("Sending {messageText} to {queueName} of account {queueAccount}", messageText, queueName, this.queueServiceClient.AccountName);
            return this.queueServiceClient.GetQueueClient(queueName).SendMessageAsync(messageText).Map(result => new QueueSendResponse
            {
                MessageId = result.Value.MessageId,
                DeleteHandle = result.Value.PopReceipt,
                InsertedAt = result.Value.InsertionTime
            });
        }
    }
}
