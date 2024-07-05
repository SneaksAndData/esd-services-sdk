using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using Amazon.S3;
using Amazon.S3.Model;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Helpers;
using Snd.Sdk.Tasks;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Storage.Models.BlobPath;

namespace Snd.Sdk.Storage.Amazon;

/// <summary>
/// Blob Service implementation for S3-compatible object storage.
/// Blob path for this service should be in format s3://bucket-name/path
/// </summary>
public class AmazonBlobStorageClient : IBlobStorageWriter, IBlobStorageListService
{
    private readonly IAmazonS3 client;
    private readonly ILogger<AmazonBlobStorageClient> logger;

    /// <summary>
    /// Creates a new instance of S3BlobStorageService.
    /// </summary>
    /// <param name="client">Authenticated S3 AWS client instance</param>
    /// <param name="logger">Logger</param>
    public AmazonBlobStorageClient(IAmazonS3 client, ILogger<AmazonBlobStorageClient> logger)
    {
        this.client = client;
        this.logger = logger;
    }

    /// <inheritdoc/>
    public Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, string blobPath, string blobName, bool overwrite = false)
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

    /// <inheritdoc/>
    public Source<StoredBlob, NotUsed> ListBlobs(string blobPath)
    {
        var path = blobPath.AsAmazonS3Path();
        return Source.From(() => this.GetObjectsPaginator(path)).Select(this.MapToStoredBlob);
    }

    /// <inheritdoc/>
    public IEnumerable<StoredBlob> ListBlobsAsEnumerable(string blobPath)
    {
        var path = blobPath.AsAmazonS3Path();
        var request = new ListObjectsV2Request
        {
            BucketName = path.Bucket,
            Prefix = path.ObjectKey
        };
        do
        {
            var response = this.client.ListObjectsV2Async(request).GetAwaiter().GetResult();
            foreach (var s3Object in response.S3Objects)
            {
                yield return this.MapToStoredBlob(s3Object);
            }
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }

    private StoredBlob MapToStoredBlob(S3Object arg)
    {
        return new StoredBlob
        {
            Name = arg.Key,
            LastModified = arg.LastModified,
            ContentLength = arg.Size,
            ContentHash = arg.ETag
        };
    }

    private IAsyncEnumerable<S3Object> GetObjectsPaginator(AmazonS3StoragePath path)
    {
        var paginator = this.client.Paginators.ListObjectsV2(new ListObjectsV2Request
        {
            BucketName = path.Bucket,
            Prefix = path.ObjectKey
        });
        return paginator.S3Objects;
    }

}
