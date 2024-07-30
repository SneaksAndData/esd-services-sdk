using System;
using System.Text.RegularExpressions;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Models.BlobPath;

/// <summary>
/// Amazon S3 path.
/// </summary>
public record AmazonS3StoragePath : IStoragePath
{
    private const string matchRegex = "s3a://(?<bucket>[^/]+)/?(?<key>.*)";

    /// <summary>
    /// Blob bucket name
    /// </summary>
    public string Bucket { get; init; }

    /// <inheritdoc cref="IStoragePath.ObjectKey"/>
    public string ObjectKey { get; init; }

    /// <inheritdoc cref="IStoragePath.ToHdfsPath"/>
    public string ToHdfsPath() => $"s3a://{this.Bucket}/{this.ObjectKey}";

    /// <inheritdoc cref="IStoragePath"/>
    public AmazonS3StoragePath Join(string keyName)
    {
        return this with
        {
            ObjectKey = string.IsNullOrEmpty(this.ObjectKey) ? keyName : $"{this.ObjectKey}/{keyName.TrimStart('/')}"
        };
    }


    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AmazonS3StoragePath"/>.
    /// </summary>
    /// <param name="hdfspath">HDFS path in format s3a://bucket/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public AmazonS3StoragePath(string hdfspath)
    {
        var regex = new Regex(matchRegex);
        var match = regex.Match(hdfspath);

        if (!match.Success)
        {
            throw new ArgumentException($"An {nameof(AmazonS3StoragePath)} must be in the format s3a://bucket/path, but was: {hdfspath}");
        }

        this.Bucket = match.Groups["bucket"].Value;
        this.ObjectKey = match.Groups["key"].Value;
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="AmazonS3StoragePath"/> from bucket and object key.
    /// <param name="bucket">Bucket name</param>
    /// <param name="objectKey">Object key</param>
    /// </summary>
    public AmazonS3StoragePath(string bucket, string objectKey)
    {
        this.Bucket = bucket;
        this.ObjectKey = objectKey;
    }

    /// <summary>
    /// Tests is path can be converted to <see cref="AmazonS3StoragePath"/>
    /// </summary>
    /// <param name="hdfsPath">Path to check</param>
    /// <returns>True if path con be converted to <see cref="AmazonS3StoragePath"/></returns>
    public static bool IsAmazonS3Path(string hdfsPath) => new Regex(matchRegex).IsMatch(hdfsPath);
    
}
