using Akka;
using Akka.IO;
using Akka.Streams.Dsl;
using Akka.Streams.IO;
using Azure;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Azure
{
    /// <summary>
    /// Implementation of a shared filesystem for Azure Cloud.
    /// </summary>
    public class AzureSharedFSService : ISharedFileSystemService
    {
        private readonly ShareServiceClient shareServiceClient;
        private readonly ILogger<AzureSharedFSService> logger;

        /// <summary>
        /// Create an instance of <see cref="AzureSharedFSService"/>.
        /// </summary>
        /// <param name="shareServiceClient">Azure Shares service client.</param>
        /// <param name="logger">Logger instance for this class.</param>
        public AzureSharedFSService(ShareServiceClient shareServiceClient, ILogger<AzureSharedFSService> logger)
        {
            this.shareServiceClient = shareServiceClient;
            this.logger = logger;
        }

        /// <inheritdoc />
        public Source<ShareFile, NotUsed> ListFiles(string fileSystemName, string path)
        {
            this.logger.LogDebug("Listing files from {fileSystemName} under {path}", fileSystemName, path);
            var dirClient = this.shareServiceClient.GetShareClient(fileSystemName).GetDirectoryClient(path);
            return Source.From(dirClient.GetFilesAndDirectories(prefix: string.Empty))
                .Where(fileOrDir => !fileOrDir.IsDirectory)
                .Select(file => new ShareFile
                {
                    Name = file.Name,
                    Size = file.FileSize.GetValueOrDefault(0L),
                    ShareItemId = file.Id,
                    CreatedOn = file.Properties?.CreatedOn,
                    LastModifiedOn = file.Properties?.LastModified
                });
        }

        /// <inheritdoc />
        public Source<ByteString, Task<IOResult>> ReadTextFile(string fileSystemName, string path, string fileName, int bufferSize = 4194304)
        {
            var dirClient = this.shareServiceClient.GetShareClient(fileSystemName).GetDirectoryClient(path);
            var fileClient = dirClient.GetFileClient(fileName);

            return StreamConverters.FromInputStream(() =>
            {
                try
                {
                    return fileClient.OpenRead(allowfileModifications: true, bufferSize: bufferSize);
                }
                catch (RequestFailedException ex)
                {
                    this.logger.LogWarning(ex, "Failed to read logs for {fileName}", fileName);
                    return new MemoryStream(Encoding.UTF8.GetBytes($"Not found: {fileName}\n"));
                }
            }, chunkSize: (int)(bufferSize * 0.9));
        }

        /// <inheritdoc />
        public Task<bool> RemoveFile(string fileSystemName, string path, string fileName)
        {
            var dirClient = this.shareServiceClient.GetShareClient(fileSystemName).GetDirectoryClient(path);
            var fileClient = dirClient.GetFileClient(fileName);

            try
            {
                return fileClient.DeleteAsync().Map(_ => true);
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogError(ex, "Failed to delete {path}/{fileName}", path, fileName);
                return Task.FromResult(false);
            }
        }
    }
}
