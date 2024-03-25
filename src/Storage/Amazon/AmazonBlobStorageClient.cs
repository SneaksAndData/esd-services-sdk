using System;
using System.IO;
using System.Threading.Tasks;
using Amazon.S3;
using Amazon.S3.Model;
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

    /// <summary>
    /// Creates a new instance of S3BlobStorageService.
    /// </summary>
    /// <param name="client">Authenticated S3 AWS client instance</param>
    public AmazonBlobStorageWriter(IAmazonS3 client)
    {
        this.client = client;
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
        return client.PutObjectAsync(request).Map(result => new UploadedBlob
        {
            Name = blobName,
            ContentHash = result.ChecksumSHA256,
            LastModified = DateTimeOffset.UtcNow
        });
    }
}
