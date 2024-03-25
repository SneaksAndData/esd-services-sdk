using Akka.IO;
using Akka.Streams.Dsl;
using Azure;
using Azure.Storage.Files.Shares;
using Azure.Storage.Files.Shares.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Snd.Sdk.Storage.Azure;
using Snd.Sdk.Storage.Models;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Snd.Sdk.Tests.CustomMocks;
using Xunit;

namespace Snd.Sdk.Tests.Storage.Azure
{
    public class AzureSharedFSServiceTests : IClassFixture<AkkaFixture>, IClassFixture<LoggerFixture>
    {
        private readonly AkkaFixture akkaFixture;
        private readonly LoggerFixture loggerFixture;
        private readonly Mock<ShareServiceClient> mockServiceClient;
        private readonly AzureSharedFSService azureFsService;

        public AzureSharedFSServiceTests(AkkaFixture akkaFixture, LoggerFixture loggerFixture)
        {
            this.akkaFixture = akkaFixture;
            this.loggerFixture = loggerFixture;
            this.mockServiceClient = new Mock<ShareServiceClient>();
            this.azureFsService = new AzureSharedFSService(this.mockServiceClient.Object, this.loggerFixture.Factory.CreateLogger<AzureSharedFSService>());
        }

        [Theory]
        [InlineData("testshare", "path/to/files")]
        public async Task ListFiles(string fileSystemName, string path)
        {
            var mockSc = new Mock<ShareClient>();
            var mockDc = new Mock<ShareDirectoryClient>();
            var pages = Pageable<ShareFileItem>.FromPages(Enumerable.Range(0, 10).Select(ixp => Page<ShareFileItem>.FromValues(Enumerable.Range(0, 10).Select(ix => FilesModelFactory.ShareFileItem(name: ix.ToString())).ToList(), ixp > 9 ? null : ixp.ToString(), new MockAzureResponse(200))));

            mockSc.Setup(msc => msc.GetDirectoryClient(path)).Returns(mockDc.Object);
            mockDc.Setup(mdc => mdc.GetFilesAndDirectories(string.Empty, default)).Returns(pages);
            this.mockServiceClient.Setup(msc => msc.GetShareClient(fileSystemName)).Returns(mockSc.Object);

            var result = await this.azureFsService.ListFiles(fileSystemName, path).RunWith(Sink.Seq<ShareFile>(), this.akkaFixture.Materializer);

            Assert.Equal(100, result.Count);
        }


        [Theory]
        [InlineData("testshare", "path/to/files", "fileA", "content", 4000000)]
        [InlineData("testshare", "path/to/files", "fileA", "contentcontent content contentcontent content", 100)]
        public async Task ReadTextFile(string fileSystemName, string path, string fileName, string fileContent, int bufferSize)
        {
            var testContent = Encoding.UTF8.GetBytes(fileContent);
            var mockSc = new Mock<ShareClient>();
            var mockDc = new Mock<ShareDirectoryClient>();
            var mockFc = new Mock<ShareFileClient>();

            mockSc.Setup(msc => msc.GetDirectoryClient(path)).Returns(mockDc.Object);
            mockDc.Setup(mdc => mdc.GetFileClient(fileName)).Returns(mockFc.Object);

            mockFc.Setup(mfc => mfc.OpenRead(true, 0, bufferSize, default)).Returns(new MemoryStream(testContent));
            this.mockServiceClient.Setup(msc => msc.GetShareClient(fileSystemName)).Returns(mockSc.Object);

            var result = await this.azureFsService.ReadTextFile(fileSystemName, path, fileName, bufferSize).RunWith(Sink.Seq<ByteString>(), this.akkaFixture.Materializer);
            var resultContent = string.Join("", result.Select(bs => bs.ToString()));

            Assert.Equal(fileContent, resultContent);
        }

        [Theory]
        [InlineData("testshare", "path/to/files", "fileA")]
        public async Task RemoveFile(string fileSystemName, string path, string fileName)
        {
            var mockSc = new Mock<ShareClient>();
            var mockDc = new Mock<ShareDirectoryClient>();
            var mockFc = new Mock<ShareFileClient>();

            mockSc.Setup(msc => msc.GetDirectoryClient(path)).Returns(mockDc.Object);
            mockDc.Setup(mdc => mdc.GetFileClient(fileName)).Returns(mockFc.Object);

            mockFc.Setup(mfc => mfc.DeleteAsync(null, default)).ReturnsAsync(new MockAzureResponse(200));
            this.mockServiceClient.Setup(msc => msc.GetShareClient(fileSystemName)).Returns(mockSc.Object);

            var result = await this.azureFsService.RemoveFile(fileSystemName, path, fileName);

            Assert.True(result);
        }
    }
}
