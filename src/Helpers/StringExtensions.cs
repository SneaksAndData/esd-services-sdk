using System;
using Snd.Sdk.Storage.Models.Base;
using Snd.Sdk.Storage.Models.BlobPath;

namespace Snd.Sdk.Helpers;

/// <summary>
/// Extension methods for string type.
/// </summary>
public static class StringExtensions
{
    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AdlsGen2Path"/>.
    /// </summary>
    /// <param name="path">HDFS path in format abfss://container@/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public static AdlsGen2Path AsAdlsGen2Path(this string path)
    {
        return new AdlsGen2Path(path);
    }

    /// <summary>
    /// Converts HDFS path to an instance of <see cref="AmazonS3StoragePath"/>.
    /// </summary>
    /// <param name="path">HDFS path in format abfss://container@/path</param>
    /// <returns>Path instance</returns>
    /// <exception cref="ArgumentException">If path does not match the format</exception>
    public static AmazonS3StoragePath AsAmazonS3Path(this string path)
    {
        return new AmazonS3StoragePath(path);
    }

    /// <summary>
    /// Creates a new instance of <see cref="IStoragePath"/> from HDFS path.
    /// </summary>
    /// <param name="hdfsPath">Raw path in string view</param>
    /// <returns>The instance of one of supported path types</returns>
    /// <exception cref="ArgumentException">Throws ArgumentException of the path schema is not supported or the path is malformed</exception>
    public static IStoragePath FromHdfsPath(this string hdfsPath)
    {
        if (AdlsGen2Path.IsAdlsGen2Path(hdfsPath))
        {
            return new AdlsGen2Path(hdfsPath);
        }
        if (AmazonS3StoragePath.IsAmazonS3Path(hdfsPath))
        {
            return new AmazonS3StoragePath(hdfsPath);
        }
        throw new ArgumentException($"Path {hdfsPath} is supported.");
    }
}
