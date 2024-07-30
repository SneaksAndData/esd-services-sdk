using Akka;
using Akka.IO;
using Akka.Streams.Dsl;
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Tasks;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Snd.Sdk.Helpers;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Storage.Models.BlobPath;

namespace Snd.Sdk.Storage.Azure
{
    /// <summary>
    /// Blob Service implementation for Azure.
    /// Blob path for this service should be in format container@my/blob/path
    /// </summary>
    public class AzureBlobStorageService : IBlobStorageService<AdlsGen2Path>
    {
        private readonly BlobServiceClient blobServiceClient;
        private readonly ILogger<AzureBlobStorageService> logger;

        private BlobClient GetBlobClient(string blobPath, string blobName)
        {
            var adlsGen2Path = blobPath.AsAdlsGen2Path();
            var bcc = this.blobServiceClient.GetBlobContainerClient(adlsGen2Path.Container);
            return bcc.GetBlobClient($"{adlsGen2Path.ObjectKey}/{blobName}");
        }

        /// <summary>
        /// Creates an instance of <see cref="AzureBlobStorageService"/>.
        /// </summary>
        /// <param name="blobServiceClient"></param>
        /// <param name="logger"></param>
        public AzureBlobStorageService(BlobServiceClient blobServiceClient, ILogger<AzureBlobStorageService> logger)
        {
            this.blobServiceClient = blobServiceClient;
            this.logger = logger;
        }

        /// <inheritdoc />
        public T GetBlobContent<T>(AdlsGen2Path path, Func<BinaryData, T> deserializer)
        {
            var blobPath = path.BlobPath;
            var blobName = path.BlobName;
            var bc = GetBlobClient(blobPath, blobName);
            try
            {
                var content = bc.DownloadContent().Value.Content;
                return deserializer(content);
            }
            catch (RequestFailedException rfex)
            {
                this.logger.LogError(rfex, "File {blobName} does not exist under {blobPath}.", blobName, blobPath);
                return default;
            }
            catch (JsonException jex)
            {
                this.logger.LogError(jex,
                    "Content of {blobName} under {blobPath} is not a valid json. Specify a different serializer or check blob contents.",
                    blobName, blobPath);
                return default;
            }
            catch (Exception other)
            {
                this.logger.LogError(other, "Failed to process content of {blobName} under {blobPath}.", blobName,
                    blobPath);
                return default;
            }
        }

        /// <inheritdoc />
        public Task<T> GetBlobContentAsync<T>(AdlsGen2Path adlsGen2Path, Func<BinaryData, T> deserializer)
        {
            var blobPath = adlsGen2Path.BlobPath;
            var blobName = adlsGen2Path.BlobName;
            var bc = GetBlobClient(blobPath, blobName);
            try
            {
                return bc.DownloadContentAsync().Map(result => deserializer(result.Value.Content));
            }
            catch (RequestFailedException rfex)
            {
                this.logger.LogError(rfex, "File {blobName} does not exist under {blobPath}.", blobName, blobPath);
                return Task.FromResult(default(T));
            }
            catch (JsonException jex)
            {
                this.logger.LogError(jex,
                    "Content of {blobName} under {blobPath} is not a valid json. Specify a different serializer or check blob contents.",
                    blobName, blobPath);
                return Task.FromResult(default(T));
            }
            catch (Exception other)
            {
                this.logger.LogError(other, "Failed to process content of {blobName} under {blobPath}.", blobName,
                    blobPath);
                return Task.FromResult(default(T));
            }
        }

        /// <inheritdoc />
        public Stream StreamBlobContent(string blobPath, string blobName)
        {
            var bc = GetBlobClient(blobPath, blobName);
            return bc.OpenRead(new BlobOpenReadOptions(true));
        }

        /// <inheritdoc />
        public IDictionary<string, string> GetBlobMetadata(string blobPath, string blobName)
        {
            try
            {
                return GetBlobClient(blobPath, blobName).GetProperties().Value.Metadata;
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogWarning(exception: ex,
                    message: "Unable to get metadata for a blob {blobName} on {blobPath}", blobName, blobPath);
                return default;
            }
            catch (Exception other)
            {
                this.logger.LogError(exception: other,
                    message: "Fatal error when reading metadata for a blob {blobName} on {blobPath}", blobName, blobPath);
                return default;
            }
        }

        /// <inheritdoc />
        public Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobPath, string blobName)
        {
            try
            {
                return GetBlobClient(blobPath, blobName).GetPropertiesAsync().Map(props => props.Value.Metadata);
            }
            catch (RequestFailedException ex)
            {
                this.logger.LogWarning(exception: ex,
                    message: "Unable to get metadata for a blob {blobName} on {blobPath}", blobName, blobPath);
                return Task.FromResult(default(IDictionary<string, string>));
            }
            catch (Exception other)
            {
                this.logger.LogError(exception: other,
                    message: "Fatal error when reading metadata for a blob {blobName} on {blobPath}", blobName, blobPath);
                return Task.FromResult(default(IDictionary<string, string>));
            }
        }

        /// <inheritdoc />
        [ExcludeFromCodeCoverage]
        // Requires an additional parameter ("validForSeconds", 123) to set SAS expiry date. Defaults to 1 minute if not provided.
        public Uri GetBlobUri(string blobPath, string blobName, params ValueTuple<string, object>[] kwOptions)
        {
            var blobClient = GetBlobClient(blobPath, blobName);
            var sasDuration = kwOptions.Where(opt => opt.Item1 == "validForSeconds").ToList();

            return blobClient.GenerateSasUri(BlobSasPermissions.Read,
                DateTimeOffset.UtcNow.AddSeconds(sasDuration.Count == 0 ? 60 : (double)sasDuration.First().Item2));
        }

        private Pageable<BlobItem> ListBlobItems(string blobPath)
        {
            var containerClient = this.blobServiceClient.GetBlobContainerClient(blobPath.AsAdlsGen2Path().Container);
            return containerClient.GetBlobs(prefix: blobPath.AsAdlsGen2Path().ObjectKey);
        }

        private StoredBlob MapBlobItem(BlobItem blobItem)
        {
            return new StoredBlob
            {
                Metadata = blobItem.Metadata,
                Name = blobItem.Name,
                ContentEncoding = blobItem.Properties?.ContentEncoding,
                ContentHash = blobItem.Properties?.ContentHash != null
                    ? Encoding.UTF8.GetString(blobItem.Properties.ContentHash)
                    : null,
                LastModified = blobItem.Properties.LastModified,
                CreatedOn = blobItem.Properties.CreatedOn,
                ContentType = blobItem.Properties.ContentType,
                ContentLength = blobItem.Properties.ContentLength
            };
        }

        /// <inheritdoc />
        public Source<StoredBlob, NotUsed> ListBlobs(string blobPath)
        {
            return Source.From(ListBlobItems(blobPath)).Select(MapBlobItem);
        }

        /// <inheritdoc />
        public IEnumerable<StoredBlob> ListBlobsAsEnumerable(string blobPath)
        {
            return ListBlobItems(blobPath).Select(MapBlobItem);
        }

        /// <inheritdoc />
        public Task<bool> MoveBlob(string sourcePath, string sourceName, string targetPath, string targetBlobName)
        {
            var sourceBlobClient = GetBlobClient(sourcePath, sourceName);
            var targetBlobClient = GetBlobClient(targetPath, targetBlobName);

            return targetBlobClient
                .SyncCopyFromUriAsync(sourceBlobClient.GenerateSasUri(BlobSasPermissions.Read,
                    DateTimeOffset.UtcNow.AddMinutes(5)))
                .Map(result =>
                {
                    if (result.Value.CopyStatus == CopyStatus.Success)
                    {
                        return sourceBlobClient.DeleteIfExistsAsync().Map(dr => dr.Value);
                    }

                    return Task.FromResult(false);
                })
                .Flatten();
        }

        /// <inheritdoc />
        public Task<bool> RemoveBlob(AdlsGen2Path path)
        {
            var blobPath = path.BlobPath;
            var blobName = path.BlobName;
            var blobClient = GetBlobClient(blobPath, blobName);
            return blobClient.DeleteIfExistsAsync().Map(v => v.Value);
        }

        /// <inheritdoc />
        public Task<UploadedBlob> SaveTextAsBlob(string text, AdlsGen2Path path)
        {
            var blobName = path.BlobName;
            var adlsPath = path;
            var containerClient = this.blobServiceClient.GetBlobContainerClient(adlsPath.Container);

            return containerClient
                .UploadBlobAsync(blobName: $"{adlsPath.ObjectKey}/{blobName}",
                    content: new BinaryData(Encoding.UTF8.GetBytes(text))).Map(result => new UploadedBlob
                    {
                        Name = $"{adlsPath.ObjectKey}/{blobName}",
                        ContentHash = Encoding.UTF8.GetString(result.Value.ContentHash),
                        LastModified = result.Value.LastModified
                    });
        }

        /// <summary>  
        /// Gets an instance of <see cref="AppendBlobClient"/> for a specified blob path and blob name.  
        /// If the blob doesn't exist, it will be created.  
        /// </summary>  
        /// <param name="blobPath">The path of the blob in Azure Data Lake Storage Gen2.</param>  
        /// <param name="blobName">The name of the blob to retrieve.</param>  
        /// <returns>Returns an instance of <see cref="AppendBlobClient"/> for the specified blob.</returns>
        [ExcludeFromCodeCoverage]
        protected virtual AppendBlobClient GetAppendBlobClient(string blobPath, string blobName)
        {
            var adlsPath = blobPath.AsAdlsGen2Path();
            var blobClient = this.blobServiceClient.GetBlobContainerClient(adlsPath.Container)
                .GetAppendBlobClient($"{adlsPath.ObjectKey}/{blobName}");

            blobClient.CreateIfNotExists();

            return blobClient;
        }

        /// <inheritdoc />
        public Flow<ByteString, bool, NotUsed> StreamToBlob(string blobPath, string blobName)
        {
            var blobClient = GetAppendBlobClient(blobPath, blobName);

            return Flow.Create<ByteString, NotUsed>()
                .SelectAsync(1, block =>
                {
                    var blockStream = new MemoryStream((byte[])block);

                    return blobClient.AppendBlockAsync(blockStream).Map(_ => true);
                })
                .RecoverWithRetries(ex =>
                {
                    this.logger.LogError(ex, "Failed to append a block to blob {blobName} under path {blobPath}.",
                        blobName, blobPath);
                    return Source.Single(false);
                }, 1)
                .Aggregate(true, (agg, element) => agg && element);
        }

        /// <inheritdoc />
        public Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, AdlsGen2Path path, bool overwrite = false)
        {
            var blobPath = path.BlobPath;
            var blobName = path.BlobName;
            var blobClient = GetBlobClient(blobPath, blobName);

            return blobClient.UploadAsync(bytes, overwrite: overwrite).Map(result => new UploadedBlob
            {
                Name = blobName,
                ContentHash = Convert.ToBase64String(result.Value.ContentHash),
                LastModified = result.Value.LastModified
            });
        }
    }
}
