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
    private const string matchRegex = @"^(abfss://)?(?<container>[^@:/]+)@(?<key>.+)$";

    /// <summary>
    /// Blob container name
    /// </summary>
    public string Container { get; init; }


    /// <inheritdoc cref="IStoragePath"/>
    public string ObjectKey { get; init; }
    
    /// <inheritdoc cref="IStoragePath"/>
    public string BlobName => string.IsNullOrEmpty(this.ObjectKey) ? "" : this.ObjectKey.Split("/").Last();

    /// <inheritdoc cref="IStoragePath"/>
    public string BlobPath => 
        string.IsNullOrEmpty(this.ObjectKey)
            ? $"abfss://{Container}@"
            : $"abfss://{Container}@" + string.Join("/", this.ObjectKey.Split("/").ToArray()[..^1]);

    /// <inheritdoc cref="IStoragePath"/>
    public IStoragePath Join(string keyName)
    {
        return this with
        {
            ObjectKey = $"{this.ObjectKey.Trim('/')}/{keyName.Trim('/')}"
        };
    }

    /// <inheritdoc cref="IStoragePath.ToHdfsPath"/>
    public string ToHdfsPath() => $"abfss://{this.Container}@{this.ObjectKey.Trim('/')}";

    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AdlsGen2Path"/>.
    /// </summary>
    /// <param name="hdfsPath">HDFS path in format abfss://container@/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public AdlsGen2Path(string hdfsPath)
    {
        var regex = new Regex(matchRegex);
        var match = regex.Match(hdfsPath);

        if (!match.Success)
        {
            throw new ArgumentException(
                $"An {nameof(AdlsGen2Path)} must be in the format abfss://container@path/to/key, but was: {hdfsPath}");
        }

        Container = match.Groups["container"].Value;
        ObjectKey = match.Groups["key"].Value;
    }
    
    public AdlsGen2Path(string blobPath, string blobName)
    {
        var regex = new Regex(matchRegex);
        var match = regex.Match($"{blobPath}/{blobName}");

        if (!match.Success)
        {
            throw new ArgumentException(
                $"An {nameof(AdlsGen2Path)} must be in the format abfss://container@path/to/key, but was: {blobPath}, {blobName}");
        }

        Container = match.Groups["container"].Value;
        ObjectKey = match.Groups["key"].Value;
    }

    /// <summary>
    /// Tests is path can be converted to <see cref="AdlsGen2Path"/>
    /// </summary>
    /// <param name="hdfsPath">Path to check</param>
    /// <returns>True if path con be converted to <see cref="AdlsGen2Path"/></returns>
    public static bool IsAdlsGen2Path(string hdfsPath) => new Regex(matchRegex).IsMatch(hdfsPath);
}
