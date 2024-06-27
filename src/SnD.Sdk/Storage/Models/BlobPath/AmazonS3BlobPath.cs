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

    /// <summary>
    /// Blob bucket name
    /// </summary>
    public string Bucket { get; init; }

    /// <inheritdoc cref="IStoragePath"/>
    public string ObjectKey { get; init; }

    /// <inheritdoc cref="IStoragePath"/>
    public IStoragePath Join(string keyName)
    {
        return this with
        {
            ObjectKey = $"{this.ObjectKey}/{keyName}"
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

        Bucket = match.Groups["bucket"].Value;
        ObjectKey = match.Groups["key"].Value;
    }

    /// <summary>
    /// Tests is path can be converted to <see cref="AmazonS3StoragePath"/>
    /// </summary>
    /// <param name="hdfsPath">Path to check</param>
    /// <returns>True if path con be converted to <see cref="AmazonS3StoragePath"/></returns>
    public static bool IsAmazonS3Path(string hdfsPath) => new Regex(matchRegex).IsMatch(hdfsPath);
}
