using System;
using System.Text.RegularExpressions;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Models.BlobPath;

/// <summary>
/// Amazon S3 path.
/// </summary>
public record AmazonS3StoragePath : IStoragePath
{
    private const string matchRegex = "s3a://(?<bucket>[^/]+)/(?<key>.*)";
    private readonly string objectKey;

    /// <summary>
    /// Blob bucket name
    /// </summary>
    public string Bucket { get; init; }

    /// <inheritdoc cref="IStoragePath.ObjectKey"/>
    public string ObjectKey
    {
        get => this.objectKey;
        init => this.objectKey = Regex.Replace(value.Trim('/'), "/+", "/");
    }

    /// <inheritdoc cref="IStoragePath.ToHdfsPath"/>
    public string ToHdfsPath() => $"s3a://{this.Bucket}/{this.ObjectKey}";

    /// <inheritdoc cref="IStoragePath"/>
    public IStoragePath Join(string keyName)
    {
        return this with
        {
            ObjectKey = $"{this.ObjectKey}/{keyName.Trim('/')}"
        };
    }


    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AmazonS3StoragePath"/>.
    /// </summary>
    /// <param name="hdfspath">HDFS path in format abfss://container@/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public AmazonS3StoragePath(string hdfspath)
    {
        var regex = new Regex(matchRegex);
        var match = regex.Match(hdfspath);

        if (!match.Success)
        {
            throw new ArgumentException($"An {nameof(AmazonS3StoragePath)} must be in the format s3a://bucket/path");
        }

        this.Bucket = match.Groups["bucket"].Value;
        this.ObjectKey = match.Groups["key"].Value;
    }

    /// <summary>
    /// Tests is path can be converted to <see cref="AmazonS3StoragePath"/>
    /// </summary>
    /// <param name="hdfsPath">Path to check</param>
    /// <returns>True if path con be converted to <see cref="AmazonS3StoragePath"/></returns>
    public static bool IsAmazonS3Path(string hdfsPath) => new Regex(matchRegex).IsMatch(hdfsPath);
}
