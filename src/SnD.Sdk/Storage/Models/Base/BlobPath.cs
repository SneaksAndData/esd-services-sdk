namespace Snd.Sdk.Storage.Models.Base;

/// <summary>
/// Type-safe abstraction of different storage protocol paths.
/// </summary>
public interface IStoragePath
{
    /// <summary>
    /// Converts the given path to HDFS path string.
    /// </summary>
    /// <returns>String representing HDFS path information</returns>
    public string ToHdfsPath();
}
