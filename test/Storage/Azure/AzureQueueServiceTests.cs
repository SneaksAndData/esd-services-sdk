using Akka.Streams.Dsl;
using Azure;
using Azure.Storage.Queues;
using Azure.Storage.Queues.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Azure;
using Snd.Sdk.Storage.Models;
using System;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snd.Sdk.Tests.CustomMocks;
using Xunit;

namespace Snd.Sdk.Tests.Storage.Azure
{
    public class AzureQueueServiceTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;
        private readonly Mock<QueueServiceClient> mockServiceClient;
        private readonly AzureQueueService azureQueueService;

        public AzureQueueServiceTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
            this.mockServiceClient = new Mock<QueueServiceClient>();
            this.azureQueueService = new AzureQueueService(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AzureQueueService>());
        }

        [Theory]
        [InlineData(10, 60, 10)]
        [InlineData(10, 60, 1)]
        [InlineData(10, 10, 1)]
        public async Task GetQueueMessages(int messagesPerCall, int visibilityTimeoutSeconds, int numCalls)
        {
            var mockQueue = new Mock<QueueClient>();
            var mockMessages = Enumerable.Range(0, messagesPerCall).Select(ix => QueuesModelFactory.QueueMessage(ix.ToString(), Guid.NewGuid().ToString(), new BinaryData(Encoding.UTF8.GetBytes(ix.ToString())), 3)).ToArray();
            mockQueue.Setup(mq => mq.ReceiveMessagesAsync(messagesPerCall, TimeSpan.FromSeconds(visibilityTimeoutSeconds), default)).ReturnsAsync(Response.FromValue(mockMessages, new MockAzureResponse(200)));
            this.mockServiceClient.Setup(msc => msc.GetQueueClient("test")).Returns(mockQueue.Object);

            var result = await this.azureQueueService.GetQueueMessages("test", TimeSpan.FromSeconds(visibilityTimeoutSeconds), messagesPerCall, TimeSpan.FromSeconds(10)).Take(numCalls * messagesPerCall).RunWith(Sink.Seq<QueueElement>(), this.akkaFixture.Materializer);

            Assert.Equal(messagesPerCall * numCalls, result.Count);
        }

        [Fact]
        public async Task ReleaseMessage()
        {
            var mockQueue = new Mock<QueueClient>();
            var ts = DateTimeOffset.Now;
            mockQueue.Setup(mq => mq.UpdateMessageAsync("test", "testreceipt", (string)null, TimeSpan.FromSeconds(0), default)).ReturnsAsync(Response.FromValue(QueuesModelFactory.UpdateReceipt("testreceipt", ts), new MockAzureResponse(200)));
            this.mockServiceClient.Setup(msc => msc.GetQueueClient("test")).Returns(mockQueue.Object);

            var result = await this.azureQueueService.ReleaseMessage(queueName: "test", receiptId: "testreceipt", messageId: "test");

            Assert.Equal(ts, result.VisibleAt);
        }

        [Fact]
        public async Task RemoveQueueMessage()
        {
            var mockQueue = new Mock<QueueClient>();
            mockQueue.Setup(mq => mq.DeleteMessageAsync("test", "testreceipt", default)).ReturnsAsync((Response)new MockAzureResponse(200));
            this.mockServiceClient.Setup(msc => msc.GetQueueClient("test")).Returns(mockQueue.Object);

            var result = await this.azureQueueService.RemoveQueueMessage(queueName: "test", receiptId: "testreceipt", messageId: "test");

            Assert.True(result);
        }

        [Fact]
        public async Task SendQueueMessage()
        {
            var mockQueue = new Mock<QueueClient>();
            var mockContent = Guid.NewGuid().ToString();
            var ts = DateTimeOffset.Now;
            mockQueue.Setup(mq => mq.SendMessageAsync(mockContent)).ReturnsAsync(Response.FromValue(QueuesModelFactory.SendReceipt("test", ts, ts.AddMinutes(1), "testreceipt", ts.AddSeconds(5)), new MockAzureResponse(200)));
            this.mockServiceClient.Setup(msc => msc.GetQueueClient("test")).Returns(mockQueue.Object);

            var result = await this.azureQueueService.SendQueueMessage(queueName: "test", messageText: mockContent);

            Assert.Equal("test", result.MessageId);
        }
    }
}
