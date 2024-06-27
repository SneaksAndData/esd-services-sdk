using System;
using System.IO;
using System.Threading.Tasks;
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
public class AmazonBlobStorageWriter : IBlobStorageWriter
{
    private readonly IAmazonS3 client;
    private readonly ILogger<AmazonBlobStorageWriter> logger;

    /// <summary>
    /// Creates a new instance of S3BlobStorageService.
    /// </summary>
    /// <param name="client">Authenticated S3 AWS client instance</param>
    /// <param name="logger">Logger</param>
    public AmazonBlobStorageWriter(IAmazonS3 client, ILogger<AmazonBlobStorageWriter> logger)
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
}
