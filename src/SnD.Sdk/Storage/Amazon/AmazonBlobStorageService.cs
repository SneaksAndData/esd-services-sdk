using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
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
public class AmazonBlobStorageService : IBlobStorageWriter, IBlobStorageListService, IBlobStorageReader
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
        return this.client.PutObjectAsync(request).TryMap(result => new UploadedBlob
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
        return this.client.PutObjectAsync(request).TryMap(result => new UploadedBlob
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
    public Task<bool> RemoveBlob(string blobPath, string blobName)
    {
        var path = $"{blobPath}/{blobName}".AsAmazonS3Path();
        var request = new DeleteObjectRequest
        {
            BucketName = path.Bucket,
            Key = path.ObjectKey
        };
        return this.client
            .DeleteObjectAsync(request)
            .TryMap(success => true, exception =>
            {
                this.logger.LogError("Failed to delete blob {blobName} from {bucket}", blobName, path.Bucket);
                return false;
            });
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Uri GetBlobUri(string blobPath, string blobName, params (string, object)[] kwOptions)
    {
        var path = blobPath.AsAmazonS3Path();
        var signingOptions = kwOptions.ToDictionary(opt => opt.Item1, opt => opt.Item2);
        var duration = (double)signingOptions.GetValueOrDefault("validForSeconds", 60d);
        var request = new GetPreSignedUrlRequest
        {
            BucketName = path.Bucket,
            Key = path.Join(blobName).ObjectKey,
            Expires = DateTime.UtcNow.AddSeconds(duration),
            Protocol = Protocol.HTTPS,
            Verb = HttpVerb.GET,
        };
        return new Uri(this.client.GetPreSignedURL(request));
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Source<StoredBlob, NotUsed> ListBlobs(string blobPath)
    {
        var path = blobPath.AsAmazonS3Path();
        return Source.From(() => this.GetObjectsPaginator(path)).Select(MapToStoredBlob);
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
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
                yield return MapToStoredBlob(s3Object);
            }
            request.ContinuationToken = response.NextContinuationToken;
        }
        while (request.ContinuationToken != null);
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Task<T> GetBlobContentAsync<T>(string blobPath, string blobName, Func<BinaryData, T> deserializer)
    {
        var path = $"{blobPath}/{blobName}".AsAmazonS3Path();
        var request = new GetObjectRequest
        {
            BucketName = path.Bucket,
            Key = path.ObjectKey
        };
        return this.client
            .GetObjectAsync(request)
            .TryMap(c =>
            {
                using var memoryStream = new MemoryStream();
                c.ResponseStream.CopyTo(memoryStream);
                return deserializer(new BinaryData(memoryStream.ToArray()));
            }, exception =>
            {
                this.logger.LogError(exception, "Could not download blob {blobName} from {bucket}", blobName, path.Bucket);
                return default;
            });
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public T GetBlobContent<T>(string blobPath, string blobName, Func<BinaryData, T> deserializer)
    {
        return this.GetBlobContentAsync(blobPath, blobName, deserializer).GetAwaiter().GetResult();
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Stream StreamBlobContent(string blobPath, string blobName)
    {
        var path = $"{blobName}/{blobPath}".AsAmazonS3Path();
        var request = new GetObjectRequest
        {
            BucketName = path.Bucket,
            Key = path.ObjectKey
        };
        return this.client
            .GetObjectAsync(request)
            .GetAwaiter()
            .GetResult()
            .ResponseStream;
    }

    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    private static StoredBlob MapToStoredBlob(S3Object arg)
    {
        return new StoredBlob
        {
            Name = arg.Key,
            LastModified = arg.LastModified,
            ContentLength = arg.Size,
            ContentHash = arg.ETag
        };
    }

    [ExcludeFromCodeCoverage(Justification = "Trivial")]
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
