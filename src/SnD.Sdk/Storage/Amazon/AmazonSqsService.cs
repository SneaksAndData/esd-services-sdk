using System;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using Akka.Streams.SQS;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Tasks;

namespace Snd.Sdk.Storage.Amazon;

/// <summary>
/// Queue Service implementation for AWS SQS.
/// </summary>
public class AmazonSqsService : IQueueService<AmazonSqsSendResponse, AmazonSqsReleaseResponse>
{
    private readonly IAmazonSQS client;
    private readonly ILogger<AmazonSqsService> logger;

    /// <summary>
    /// Initializes a new instance of the <see cref="AmazonSqsService"/> class.
    /// </summary>
    /// <param name="client">The Amazon SQS client.</param>
    /// <param name="logger">The logger instance.</param>
    public AmazonSqsService(IAmazonSQS client, ILogger<AmazonSqsService> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    private Task<GetQueueUrlResponse> GetQueueUrlAsync(string queueName)
    {
        return this.client.GetQueueUrlAsync(new GetQueueUrlRequest
        {
            QueueName = queueName
        });
    }

    /// <inheritdoc />
    public Task<AmazonSqsSendResponse> SendQueueMessage(string queueName, string messageText)
    {
        this.logger.LogDebug("Sending {messageText} to {queueName}", messageText, queueName);

        var messageRequest = new SendMessageRequest()
        {
            QueueUrl = GetQueueUrlAsync(queueName). GetAwaiter().GetResult().QueueUrl,
            MessageBody = messageText
        };
        return this.client.SendMessageAsync(messageRequest).Map(result => new AmazonSqsSendResponse
        { MessageId = result.MessageId, SequenceNumber = result.SequenceNumber });
    }

    /// <inheritdoc />
    public Source<QueueElement, NotUsed> GetQueueMessages(string queueName, TimeSpan visibilityTimeout,
        int prefetchCount, TimeSpan pollInterval)
    {
        this.logger.LogDebug("Creating a stream from queue: {queueName}, using visibility timeout {visibilityTimeout}",
            queueName, visibilityTimeout);
        var settings = SqsSourceSettings.Default
            .WithVisibilityTimeout(visibilityTimeout)
            .WithMaxBatchSize(Math.Min(10, prefetchCount))
            .WithWaitTime(pollInterval);

        return SqsSource.Create(this.client, GetQueueUrlAsync(queueName) .GetAwaiter().GetResult().QueueUrl, settings)
            .Select(msg =>
                new QueueElement
                {
                    Content = BinaryData.FromString(msg.Body),
                    ElementId = msg.MessageId,
                    DeleteHandle = msg.ReceiptHandle,
                    DequeueCount = long.TryParse(msg.Attributes["ApproximateReceiveCount"], out var count) ? (long?)count : null
                });
    }

    /// <inheritdoc />
    public Task<AmazonSqsReleaseResponse> ReleaseMessage(string queueName, string receiptId, string messageId)
    {
        this.logger.LogDebug("Changing visibility of {messageId} from {queueName}", messageId, queueName);
        return this.client
            .ChangeMessageVisibilityAsync(GetQueueUrlAsync(queueName) .GetAwaiter().GetResult().QueueUrl, receiptId, 0)
            .Map(result => new AmazonSqsReleaseResponse
            { MessageId = messageId, Success = result.HttpStatusCode == System.Net.HttpStatusCode.OK });
    }

    /// <inheritdoc />
    public Task<bool> RemoveQueueMessage(string queueName, string receiptId, string messageId)
    {
        var delRequest = new DeleteMessageRequest
        {
            QueueUrl = GetQueueUrlAsync(queueName).GetAwaiter().GetResult().QueueUrl,
            ReceiptHandle = receiptId
        };
        this.logger.LogDebug("Removing {messageId} from {queueName}", messageId, queueName);
        return this.client.DeleteMessageAsync(delRequest)
            .Map(result => result.HttpStatusCode == System.Net.HttpStatusCode.OK);
    }
}
