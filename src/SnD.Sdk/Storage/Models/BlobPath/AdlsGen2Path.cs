using System;
using System.Linq;
using System.Text.RegularExpressions;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Models.BlobPath;

/// <summary>
/// Azure Data Lake Storage Gen2 path.
/// </summary>
public record AdlsGen2Path : IStoragePath
{
    private const string MATCH_REGEX = @"^(abfss://)?(?<container>[^@:/]+)@(?<key>.+)$";

    /// <summary>
    /// Blob container name
    /// </summary>
    public string Container { get; init; }

    /// <summary>
    /// Returns the blob name;
    /// </summary>
    public string BlobName { get; }

    /// <summary>
    /// Returns the full path to the blob without the blob name.
    /// </summary>
    public string BlobPath { get; }
    
    /// <summary>
    /// Returns the full path to the blob including the blob name without the container.
    /// </summary>
    public string FullPath => $"{this.BlobPath}/{this.BlobName}";

    /// <inheritdoc cref="IStoragePath.ToHdfsPath"/>
    public string ToHdfsPath() => $"abfss://{this.Container}@{this.BlobPath.Trim('/')}/{this.BlobName.Trim('/')}";

    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AdlsGen2Path"/>.
    /// </summary>
    /// <param name="hdfsPath">HDFS path in format abfss://container@/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public AdlsGen2Path(string hdfsPath)
    {
        var regex = new Regex(MATCH_REGEX);
        var match = regex.Match(hdfsPath);

        if (!match.Success)
        {
            throw new ArgumentException(
                $"An {nameof(AdlsGen2Path)} must be in the format abfss://container@path/to/key, but was: {hdfsPath}");
        }

        this.Container = match.Groups["container"].Value;

        var path = match.Groups["key"].Value.Split('/');
        this.BlobPath = string.Join("/", path[..^1]);
        this.BlobName = path[^1];
    }
    
    /// <summary>
    /// Creates a new instance of <see cref="AdlsGen2Path"/> from container and object key.
    /// </summary>
    /// <param name="blobPath">HDFS path in format abfss://container@/path</param>
    /// <param name="blobName">Blob name as string</param>
    /// <exception cref="ArgumentException"></exception>
    public AdlsGen2Path(string blobPath, string blobName)
    {
        var regex = new Regex(MATCH_REGEX);
        var match = regex.Match(blobPath);

        if (!match.Success)
        {
            throw new ArgumentException(
                $"An {nameof(AdlsGen2Path)} must be in the format abfss://container@path/to/key, but was: {blobPath}, {blobName}");
        }

        this.Container = match.Groups["container"].Value;
        this.BlobPath = match.Groups["key"].Value;
        this.BlobName = blobName;
    }

    /// <summary>
    /// Tests is path can be converted to <see cref="AdlsGen2Path"/>
    /// </summary>
    /// <param name="hdfsPath">Path to check</param>
    /// <returns>True if path con be converted to <see cref="AdlsGen2Path"/></returns>
    public static bool IsAdlsGen2Path(string hdfsPath) => new Regex(MATCH_REGEX).IsMatch(hdfsPath);
}
