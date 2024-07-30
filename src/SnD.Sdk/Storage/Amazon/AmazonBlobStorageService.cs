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
public class AmazonBlobStorageService : IBlobStorageWriter<AmazonS3StoragePath>,
    IBlobStorageListService, IBlobStorageReader<AmazonS3StoragePath>
{
    private readonly IAmazonS3 client;
    private readonly ILogger<AmazonBlobStorageService> logger;
    private const double DEFAULT_SIGNED_URL_VALIDITY_SECONDS = 60d;

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
    public Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, AmazonS3StoragePath blobPath, bool overwrite = false)
    {
        var ms = new MemoryStream();
        ms.Write(bytes);
        var request = new PutObjectRequest
        {
            BucketName = blobPath.Bucket,
            Key = blobPath.ObjectKey,
            InputStream = ms,
            AutoCloseStream = true
        };
        return this.client.PutObjectAsync(request).TryMap(result => new UploadedBlob
        {
            Name = blobPath.ObjectKey,
            ContentHash = result.ChecksumSHA256,
            LastModified = DateTimeOffset.UtcNow
        }, exception =>
        {
            this.logger.LogError(exception, "Could not upload blob {blobName} to {bucket}",
                blobPath.ObjectKey,
                blobPath.Bucket);
            return default;
        });
    }

    /// <inheritdoc/>
    public Task<UploadedBlob> SaveTextAsBlob(string text, AmazonS3StoragePath blobPath)
    {
        var request = new PutObjectRequest
        {
            BucketName = blobPath.Bucket,
            Key = blobPath.ObjectKey,
            ContentBody = text,
            CalculateContentMD5Header = true,
            ChecksumAlgorithm = ChecksumAlgorithm.SHA256
        };
        return this.client.PutObjectAsync(request).TryMap(result => new UploadedBlob
        {
            Name = blobPath.ToHdfsPath(),
            ContentHash = result.ChecksumSHA256,
            LastModified = DateTimeOffset.UtcNow
        }, exception =>
        {
            this.logger.LogError(exception, "Could not upload blob {blobName} to {bucket}", blobPath.ObjectKey,
                blobPath.Bucket);
            return default;
        });
    }

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Uri GetBlobUri(string blobPath, string blobName, params (string, object)[] kwOptions)
    {
        var path = blobPath.AsAmazonS3Path();
        var signingOptions = kwOptions.ToDictionary(opt => opt.Item1, opt => opt.Item2);
        var duration = (double)signingOptions.GetValueOrDefault("validForSeconds", DEFAULT_SIGNED_URL_VALIDITY_SECONDS);
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
    public T GetBlobContent<T>(AmazonS3StoragePath blobPath, Func<BinaryData, T> deserializer)
    {
        return this.GetBlobContentAsync(blobPath, deserializer).GetAwaiter().GetResult();
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

    /// <inheritdoc/>
    [ExcludeFromCodeCoverage(Justification = "Trivial")]
    public Task<T> GetBlobContentAsync<T>(AmazonS3StoragePath blobPath, Func<BinaryData, T> deserializer)
    {
        var request = new GetObjectRequest
        {
            BucketName = blobPath.Bucket,
            Key = blobPath.ObjectKey
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
                this.logger.LogError(exception, "Could not download blob {blobName}", blobPath);
                return default;
            });
    }

    /// <inheritdoc/>
    public Task<bool> RemoveBlob(AmazonS3StoragePath blobPath)
    {
        var request = new DeleteObjectRequest
        {
            BucketName = blobPath.Bucket,
            Key = blobPath.ObjectKey
        };
        return this.client
            .DeleteObjectAsync(request)
            .TryMap(_ => true, _ =>
            {
                this.logger.LogError("Failed to delete blob {blobName} from {bucket}", blobPath.ObjectKey, blobPath.Bucket);
                return false;
            });
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
