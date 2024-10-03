using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka.Streams.Dsl;
using Amazon.SQS;
using Amazon.SQS.Model;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Amazon;
using Snd.Sdk.Storage.Models;
using Xunit;

namespace Snd.Sdk.Tests.Storage;

public class AmazonSqsServiceTests: IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
{
    private readonly AkkaFixture akkaFixture;
    private readonly LoggerFixture loggerFixture;
    private readonly Mock<IAmazonSQS> mockServiceClient;
    private readonly AmazonSqsService amazonSqsService;


    public AmazonSqsServiceTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
    {
        this.akkaFixture = akkaFixture;
        this.loggerFixture = loggerFixture;
        this.mockServiceClient = new Mock<IAmazonSQS>();
        this.amazonSqsService = new AmazonSqsService(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AmazonSqsService>());
    }

    [Theory]
    [InlineData(10, 60, 10)]
    [InlineData(10, 60, 1)]
    [InlineData(10, 10, 1)]
    public async Task GetQueueMessages1(int messagesPerCall, int visibilityTimeoutSeconds, int numCalls)
    {
        var queueUrl = "test-queue-url";
        var mockMessages = new List<Message>();

        for (int i = 0; i < messagesPerCall; i++)
        {
            mockMessages.Add(new Message
            {
                MessageId = Guid.NewGuid().ToString(),
                ReceiptHandle = Guid.NewGuid().ToString(),
                Body = i.ToString(),
                Attributes = new Dictionary<string, string>
                {
                    { "ApproximateReceiveCount", "3" }
                }
            });
        }

        this.mockServiceClient.Setup(client => client.GetQueueUrlAsync(It.IsAny<GetQueueUrlRequest>(), default))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        this.mockServiceClient.Setup(m => m.ReceiveMessageAsync(It.Is<ReceiveMessageRequest>(req =>
                    req.QueueUrl == queueUrl &&
                    req.MaxNumberOfMessages == messagesPerCall &&
                    req.VisibilityTimeout == visibilityTimeoutSeconds),
                default))
            .ReturnsAsync(new ReceiveMessageResponse { Messages = mockMessages });

        var result = await this.amazonSqsService.GetQueueMessages(queueUrl, TimeSpan.FromSeconds(visibilityTimeoutSeconds), messagesPerCall, TimeSpan.FromSeconds(10)).Take(numCalls * messagesPerCall).RunWith(Sink.Seq<QueueElement>(), this.akkaFixture.Materializer);

        Assert.Equal(messagesPerCall * numCalls, result.Count);
    }

    [Fact]
    public async Task RemoveQueueMessage()
    {
        var queueName = "test-queue";
        var receiptId = "test-receipt-id";
        var messageId = "test-message-id";
        var queueUrl = "test-queue-url";

        this.mockServiceClient.Setup(client => client.GetQueueUrlAsync(It.IsAny<GetQueueUrlRequest>(), default))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });
        this.mockServiceClient.Setup(client => client.DeleteMessageAsync(It.IsAny<DeleteMessageRequest>(), default))
            .ReturnsAsync(new DeleteMessageResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        var result = await this.amazonSqsService.RemoveQueueMessage(queueName, receiptId, messageId);

        Assert.True(result);
    }

    [Fact]
    public async Task ReleaseMessage()
    {
        var queueName = "test-queue";
        var receiptId = "test-receipt-id";
        var messageId = "test-message-id";
        var queueUrl = "test-queue-url";


        this.mockServiceClient.Setup(client => client.GetQueueUrlAsync(It.IsAny<GetQueueUrlRequest>(), default))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });
        this.mockServiceClient.Setup(client => client.ChangeMessageVisibilityAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<int>(), default))
            .ReturnsAsync(new ChangeMessageVisibilityResponse { HttpStatusCode = System.Net.HttpStatusCode.OK });

        var result = await this.amazonSqsService.ReleaseMessage(queueName, receiptId, messageId);

        Assert.True(result.Success);
        Assert.Equal(messageId, result.MessageId);
    }

    [Fact]
    public async Task SendQueueMessage()
    {
        var queueName = "test-queue";
        var messageText = "test-message";
        var queueUrl = "test-queue-url";

        this.mockServiceClient.Setup(client => client.GetQueueUrlAsync(It.IsAny<GetQueueUrlRequest>(), default))
            .ReturnsAsync(new GetQueueUrlResponse { QueueUrl = queueUrl });

        this.mockServiceClient.Setup(client => client.SendMessageAsync(It.IsAny<SendMessageRequest>(), default))
            .ReturnsAsync(new SendMessageResponse { MessageId = "test-message-id", SequenceNumber = "12345" });

        var result = await this.amazonSqsService.SendQueueMessage(queueName, messageText);

        Assert.Equal("test-message-id", result.MessageId);
        Assert.Equal("12345", result.SequenceNumber);
    }

}
