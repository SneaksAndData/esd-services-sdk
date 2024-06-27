using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Akka;
using Akka.IO;
using Akka.Streams.Dsl;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Helpers;
using Snd.Sdk.Tasks;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Amazon;

/// <summary>
/// Blob Service implementation for S3-compatible object storage.
/// Blob path for this service should be in format s3://bucket-name/path
/// </summary>
public class AmazonBlobStorageService : IBlobStorageService
{
    private readonly IAmazonS3 client;
    private readonly ILogger<AmazonBlobStorageService> logger;

    /// <summary>
    /// Creates a new instance of S3BlobStorageService.
    /// </summary>
    /// <param name="client">Authenticated S3 AWS client instance</param>
    /// <param name="logger">Logger</param>
    public AmazonBlobStorageService(IAmazonS3 client, ILogger<AmazonBlobStorageService> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public T GetBlobContent<T>(string blobPath, string blobName, Func<BinaryData, T> deserializer)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<T> GetBlobContentAsync<T>(string blobPath, string blobName, Func<BinaryData, T> deserializer)
    {
        return this.client.GetObjectAsync(blobPath, blobName)
            .Map(result =>
            {
                using var ms = new MemoryStream();
                result.ResponseStream.CopyTo(ms);
                var binaryData = BinaryData.FromStream(ms);
                return deserializer(binaryData);
            });
    }

    /// <inheritdoc/>
    public Stream StreamBlobContent(string blobPath, string blobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IDictionary<string, string> GetBlobMetadata(string blobPath, string blobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobPath, string blobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> RemoveBlob(string blobPath, string blobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<bool> MoveBlob(string sourcePath, string sourceName, string targetPath, string targetBlobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Uri GetBlobUri(string blobPath, string blobName, params ValueTuple<string, object>[] kwOptions)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Source<StoredBlob, NotUsed> ListBlobs(string blobPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public IEnumerable<StoredBlob> ListBlobsAsEnumerable(string blobPath)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Flow<ByteString, bool, NotUsed> StreamToBlob(string blobPath, string blobName)
    {
        throw new NotImplementedException();
    }

    /// <inheritdoc/>
    public Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, string blobPath, string blobName,
        bool overwrite = false)
    {
        var path = blobPath.AsAmazonS3Path();
        var ms = new MemoryStream();
        ms.Write(bytes);
        var request = new PutObjectRequest
        {
            BucketName = path.Bucket,
            Key = path.Join(blobName).ObjectKey,
            InputStream = ms,
            AutoCloseStream = true
        };
        return client.PutObjectAsync(request).TryMap(result => new UploadedBlob
        {
            Name = blobName,
            ContentHash = result.ChecksumSHA256,
            LastModified = DateTimeOffset.UtcNow
        }, exception =>
        {
            this.logger.LogError(exception, "Could not upload blob {blobName} to {bucket}", blobName, path.Bucket);
            return default;
        });
    }

    /// <inheritdoc/>
    public Task<UploadedBlob> SaveTextAsBlob(string text, string blobPath, string blobName)
    {
        var path = blobPath.AsAmazonS3Path();
        var request = new PutObjectRequest
        {
            BucketName = path.Bucket,
            Key = path.Join(blobName).ObjectKey,
            ContentBody = text
        };
        return client.PutObjectAsync(request).TryMap(result => new UploadedBlob
        {
            Name = blobName,
            ContentHash = result.ChecksumSHA256,
            LastModified = DateTimeOffset.UtcNow
        }, exception =>
        {
            this.logger.LogError(exception, "Could not upload blob {blobName} to {bucket}", blobName, path.Bucket);
            return default;
        });
    }
}
