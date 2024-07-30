using Azure.Storage.Blobs;
using Moq;
using Snd.Sdk.Storage.Azure;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;
using Azure;
using Azure.Storage.Blobs.Models;
using System.Text.Json.Nodes;
using System.Text.Json;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Models;
using Azure.Storage.Sas;
using Azure.Storage.Blobs.Specialized;
using Akka.IO;
using System.IO;
using Snd.Sdk.Helpers;
using Snd.Sdk.Storage.Models.BlobPath;
using Snd.Sdk.Tests.CustomMocks;

namespace Snd.Sdk.Tests.Storage.Azure
{
    public enum BlobException
    {
        REQUEST_FAILED,
        JSON,
        OTHER
    }
    public class AzureBlobStorageTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;
        private readonly Mock<BlobServiceClient> mockServiceClient;
        private readonly AzureBlobStorageService azureBlobService;
        private readonly MockAzureBlobStorageService fakeAzureBlobService;

        public AzureBlobStorageTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
            this.mockServiceClient = new Mock<BlobServiceClient>();
            this.azureBlobService = new AzureBlobStorageService(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AzureBlobStorageService>());
            this.fakeAzureBlobService = new MockAzureBlobStorageService(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AzureBlobStorageService>());
        }

        [Theory]
        [InlineData("testcontainer@test/folderA", "testblob", "{\"a\": 1}", true, null)]
        [InlineData("testcontainer@test/folderA/folderB", "testblob", "some content", false, null)]
        [InlineData("testcontainer@test/folderA/folderB", "testblob", "some content", false, BlobException.REQUEST_FAILED)]
        [InlineData("testcontainer@test/folderA/folderB", "testblob", "some content", false, BlobException.JSON)]
        [InlineData("testcontainer@test/folderA/folderB", "testblob", "some content", false, BlobException.OTHER)]
        public void GetBlobContent(string blobPath, string blobName, string content, bool asJson, BlobException? exception)
        {
            var adlsPath = new AdlsGen2Path(blobPath, blobName);
            
            var mockCc = new Mock<BlobContainerClient>();
            var mockBc = new Mock<BlobClient>();
            var mockContent = Encoding.UTF8.GetBytes(content);

            switch (exception)
            {
                case BlobException.REQUEST_FAILED:
                    mockBc.Setup(mbc => mbc.DownloadContent()).Throws(new RequestFailedException("test"));
                    break;
                case BlobException.OTHER:
                    mockBc.Setup(mbc => mbc.DownloadContent()).Throws(new Exception("test other"));
                    break;
                default:
                    mockBc.Setup(mbc => mbc.DownloadContent()).Returns(Response.FromValue(BlobsModelFactory.BlobDownloadResult(new BinaryData(mockContent)), new MockAzureResponse(200)));
                    break;
            }

            mockCc.Setup(mcc => mcc.GetBlobClient($"{blobPath.AsAdlsGen2Path().FullPath}/{blobName}")).Returns(mockBc.Object);
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            if (exception.HasValue)
            {
                if (exception == BlobException.JSON)
                {
                    Assert.Null(this.azureBlobService.GetBlobContent<string>(adlsPath, (bd) => throw new JsonException("test json")));
                }
                else
                {
                    Assert.Null(this.azureBlobService.GetBlobContent<string>(adlsPath, (bd) => bd.ToString()));
                }
            }
            else if (asJson)
            {
                Assert.NotNull(this.azureBlobService.GetBlobContent(adlsPath, (bd) => JsonSerializer.Deserialize<JsonObject>(bd.ToString())));
            }
            else
            {
                Assert.NotNull(this.azureBlobService.GetBlobContent(adlsPath, (bd) => bd.ToString()));
            }
        }

        [Theory]
        [InlineData("testcontainer@test/folderA", "testblobA")]
        [InlineData("testcontainer@test/folderA/folderB", "testblobB")]
        public void GetBlobMetadata(string blobPath, string blobName)
        {
            var mockCc = new Mock<BlobContainerClient>();
            var mockBc = new Mock<BlobClient>();
            mockBc.Setup(mbc => mbc.GetProperties(null, default)).Returns(Response.FromValue(BlobsModelFactory.BlobProperties(metadata: new Dictionary<string, string> { { "testKey", "testValue" } }), new MockAzureResponse(200)));
            mockCc.Setup(mcc => mcc.GetBlobClient($"{blobPath}/{blobName}".AsAdlsGen2Path().FullPath)).Returns(mockBc.Object);
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            var result = this.azureBlobService.GetBlobMetadata(blobPath, blobName);

            Assert.Equal("testValue", result["testKey"]);
        }

        [Theory]
        [InlineData("testcontainer@test/folderA")]
        [InlineData("testcontainer@test/folderA/folderB")]
        public async Task ListBlobs(string blobPath)
        {
            var mockCc = new Mock<BlobContainerClient>();
            var mockBlobs = Enumerable.Range(0, 10)
                .Select(ixp => Page<BlobItem>.FromValues(
                    Enumerable.Range(0, 10).Select(ix => BlobsModelFactory.BlobItem(
                        ix.ToString(),
                        false,
                        BlobsModelFactory.BlobItemProperties(
                            false,
                            contentEncoding:
                            "UTF-8",
                            contentLength: 10L,
                            contentType: "text/plain",
                            lastModified: DateTimeOffset.UtcNow,
                            createdOn: DateTimeOffset.Now),
                        null,
                        null,
                        null,
                        null,
                        null,
                        null,
                        null)
                    ).ToList(), ixp == 9 ? null : ixp.ToString(), new MockAzureResponse(200)));
            mockCc.Setup(mcc => mcc.GetBlobs(BlobTraits.None, BlobStates.None, blobPath.AsAdlsGen2Path().FullPath, default)).Returns(Pageable<BlobItem>.FromPages(mockBlobs));
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            var result = await this.azureBlobService.ListBlobs(blobPath).RunWith(Sink.Seq<StoredBlob>(), this.akkaFixture.Materializer);

            Assert.Equal(100, result.Count);
        }

        [Theory]
        [InlineData("testcontainerA@test/folder", "sourceBlob", "testcontainerB@test/folder", "targetBlob")]
        public async Task MoveBlob(string sourcePath, string sourceName, string targetPath, string targetName)
        {
            var mockSourceCc = new Mock<BlobContainerClient>();
            var mockTargetCc = new Mock<BlobContainerClient>();

            var mockSourceBc = new Mock<BlobClient>();
            var mockTargetBc = new Mock<BlobClient>();
            var copyUri = new Uri($"https://{sourcePath}/{sourceName}");

            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(sourcePath.AsAdlsGen2Path().Container)).Returns(mockSourceCc.Object);
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(targetPath.AsAdlsGen2Path().Container)).Returns(mockTargetCc.Object);

            mockSourceCc.Setup(msc => msc.GetBlobClient($"{sourcePath}/{sourceName}".AsAdlsGen2Path().FullPath)).Returns(mockSourceBc.Object);
            mockTargetCc.Setup(msc => msc.GetBlobClient($"{targetPath}/{targetName}".AsAdlsGen2Path().FullPath)).Returns(mockTargetBc.Object);

            mockSourceBc.Setup(mbc => mbc.GenerateSasUri(It.IsAny<BlobSasPermissions>(), It.IsAny<DateTimeOffset>())).Returns(copyUri);
            mockSourceBc.Setup(mbc => mbc.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, default)).ReturnsAsync(Response.FromValue(true, new MockAzureResponse(200)));
            mockTargetBc.Setup(mbc => mbc.SyncCopyFromUriAsync(copyUri, null, default)).ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobCopyInfo(ETag.All, DateTimeOffset.UtcNow, "1", "test", CopyStatus.Success), new MockAzureResponse(200)));

            var result = await this.azureBlobService.MoveBlob(sourcePath, sourceName, targetPath, targetName);

            Assert.True(result);
        }

        [Theory]
        [InlineData("testcontainer@test/folderA", "testblobA")]
        [InlineData("testcontainer@test/folderA/folderB", "testblobB")]
        public async Task RemoveBlob(string blobPath, string blobName)
        {
            var mockCc = new Mock<BlobContainerClient>();
            var mockBc = new Mock<BlobClient>();
            var adlsPath = new AdlsGen2Path(blobPath, blobName);

            mockBc.Setup(mbc => mbc.DeleteIfExistsAsync(DeleteSnapshotsOption.None, null, default)).ReturnsAsync(Response.FromValue(true, new MockAzureResponse(200)));
            mockCc.Setup(mcc => mcc.GetBlobClient($"{blobPath}/{blobName}".AsAdlsGen2Path().FullPath)).Returns(mockBc.Object);
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            var result = await this.azureBlobService.RemoveBlob(adlsPath);

            Assert.True(result);
        }

        [Theory]
        [InlineData("testcontainer@test/folder", "testblob", "content")]
        public async Task SaveTextAsBlob(string blobPath, string blobName, string content)
        {
            var mockCc = new Mock<BlobContainerClient>();
            var adlsPath = new AdlsGen2Path(blobPath, blobName);

            mockCc.Setup(mcc => mcc.UploadBlobAsync(It.IsAny<string>(), It.IsAny<BinaryData>(), default)).ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, Encoding.UTF8.GetBytes(content), "1", "", "", 1), new MockAzureResponse(200)));
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            var result = await this.azureBlobService.SaveTextAsBlob(content, adlsPath);

            Assert.Equal($"{blobPath}/{blobName}".AsAdlsGen2Path().FullPath, result.Name);
        }

        [Theory]
        [InlineData("testcontainer@test/folder", "testblob")]
        public async Task StreamToBlob(string blobPath, string blobName)
        {
            var mockCc = new Mock<BlobContainerClient>();

            this.fakeAzureBlobService.MockAc.Setup(mac => mac.CreateIfNotExists(null, null, default)).Returns(Response.FromValue(BlobsModelFactory.BlobContentInfo(ETag.All, DateTimeOffset.UtcNow, new byte[] { 123 }, "1", "", "", 1), new MockAzureResponse(200)));
            this.fakeAzureBlobService.MockAc.Setup(mac => mac.AppendBlockAsync(It.IsAny<Stream>(), null, null, null, default)).ReturnsAsync(Response.FromValue(BlobsModelFactory.BlobAppendInfo(ETag.All, DateTimeOffset.UtcNow, new byte[] { 123 }, new byte[] { 123 }, "test", 1, true, "", ""), new MockAzureResponse(200)));
            this.mockServiceClient.Setup(msc => msc.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container)).Returns(mockCc.Object);

            var result = await Source.From(Enumerable.Range(0, 10).Select(ix => ByteString.FromString(ix.ToString()))).Via(this.fakeAzureBlobService.StreamToBlob(blobPath, blobName)).RunWith(Sink.First<bool>(), this.akkaFixture.Materializer);

            Assert.True(result);
        }

        class MockAzureBlobStorageService : AzureBlobStorageService
        {
            public Mock<AppendBlobClient> MockAc { get; }
            public MockAzureBlobStorageService(BlobServiceClient blobServiceClient, ILogger<AzureBlobStorageService> logger) : base(blobServiceClient, logger)
            {
                this.MockAc = new Mock<AppendBlobClient>();
            }
            protected override AppendBlobClient GetAppendBlobClient(string blobPath, string blobName)
            {
                return MockAc.Object;
            }
        }
    }
}
