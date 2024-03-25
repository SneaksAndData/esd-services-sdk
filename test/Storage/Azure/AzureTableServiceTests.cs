using Akka.Streams.Dsl;
using Azure;
using Azure.Data.Tables;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Azure;
using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snd.Sdk.Tests.CustomMocks;
using Xunit;

namespace Snd.Sdk.Tests.Storage.Azure
{
    public class AzureTableServiceTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;
        private readonly Mock<TableServiceClient> mockServiceClient;
        private readonly AzureTableService<TableEntity> azureTableService;

        public AzureTableServiceTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
            this.mockServiceClient = new Mock<TableServiceClient>();
            this.azureTableService = new AzureTableService<TableEntity>(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AzureTableService<TableEntity>>());
        }

        [Fact]
        public async Task GetEntities()
        {
            var mockClient = new Mock<TableClient>();
            var entityBatch = Enumerable.Range(0, 100).Select(ix => new TableEntity(ix.ToString(), Guid.NewGuid().ToString())).ToList();
            mockClient.Setup(mc => mc.Query<TableEntity>(It.IsAny<string>(), It.IsAny<int?>(), null, default)).Returns(Pageable<TableEntity>.FromPages(Enumerable.Range(0, 10).Select(ix => Page<TableEntity>.FromValues(entityBatch, ix > 9 ? null : ix.ToString(), new MockAzureResponse()))));
            this.mockServiceClient.Setup(msc => msc.GetTableClient("test")).Returns(mockClient.Object);

            var entities = await this.azureTableService.GetEntities("test", "test", 100).RunWith(Sink.Seq<TableEntity>(), this.akkaFixture.Materializer);

            Assert.Equal(1000, entities.Count);
        }

        [Fact]
        public async Task GetEntity()
        {
            var mockClient = new Mock<TableClient>();
            mockClient.Setup(mc => mc.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default)).ReturnsAsync(Response.FromValue(new TableEntity("testPKey", "testRKey"), new MockAzureResponse()));
            this.mockServiceClient.Setup(msc => msc.GetTableClient("test")).Returns(mockClient.Object);

            var result = await this.azureTableService.GetEntity("test", "test", "test");

            Assert.Equal("testRKey", result.RowKey);
        }

        [Theory]
        [InlineData(typeof(RequestFailedException))]
        [InlineData(typeof(ArgumentNullException))]
        [InlineData(typeof(AggregateException))]
        public async Task GetEntityRequestFailed(Type ex)
        {
            var mockClient = new Mock<TableClient>();
            switch (ex.Name)
            {
                case nameof(RequestFailedException):
                    var ta = new Func<string, Response<TableEntity>>(_ => throw new RequestFailedException("test"));
                    mockClient.Setup(mc => mc.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default)).Returns(Task.Run(() => ta("hello")));
                    break;
                case nameof(ArgumentNullException):
                    var tb = new Func<string, Response<TableEntity>>(_ => throw new ArgumentNullException(paramName: "key", message: "test"));
                    mockClient.Setup(mc => mc.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default)).Returns(Task.Run(() => tb("hello")));
                    break;
                case nameof(AggregateException):
                    var tc = new Func<string, Response<TableEntity>>(_ => throw new AggregateException("test"));
                    mockClient.Setup(mc => mc.GetEntityAsync<TableEntity>(It.IsAny<string>(), It.IsAny<string>(), null, default)).Returns(Task.Run(() => tc("hello")));
                    break;

            }

            this.mockServiceClient.Setup(msc => msc.GetTableClient("test")).Returns(mockClient.Object);

            var result = await this.azureTableService.GetEntity("test", "test", "test");

            Assert.Null(result);
        }

        [Theory]
        [InlineData(false)]
        [InlineData(true)]
        public async Task MergeEntity(bool setContentStream)
        {
            var mockEntity = new TableEntity("testPKey", "testRKey");
            var mockClient = new Mock<TableClient>();
            mockClient.Setup(mc => mc.UpsertEntityAsync(mockEntity, TableUpdateMode.Merge, default)).ReturnsAsync(
                    new MockAzureResponse(200) { ContentStream = setContentStream ? new MemoryStream(Encoding.UTF8.GetBytes(JsonSerializer.Serialize(mockEntity))) : null }
            );
            this.mockServiceClient.Setup(msc => msc.GetTableClient("test")).Returns(mockClient.Object);

            var result = await this.azureTableService.MergeEntity("test", mockEntity);

            Assert.True(result.IsSuccessful);
        }


        [Theory]
        [InlineData(200, true)]
        [InlineData(503, false)]
        public async Task DeleteEntity(int responseCode, bool expectedResult)
        {
            var mockEntity = new TableEntity("testPKey", "testRKey");
            var mockClient = new Mock<TableClient>();
            mockClient.Setup(mc => mc.DeleteEntityAsync(mockEntity.PartitionKey, mockEntity.RowKey, default, default)).ReturnsAsync(
                    new MockAzureResponse(responseCode) { }
            );
            this.mockServiceClient.Setup(msc => msc.GetTableClient("test")).Returns(mockClient.Object);

            var result = await this.azureTableService.DeleteEntity("test", mockEntity);

            Assert.Equal(expectedResult, result);
        }
    }
}
