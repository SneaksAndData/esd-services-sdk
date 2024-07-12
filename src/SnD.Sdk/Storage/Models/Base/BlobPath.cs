namespace Snd.Sdk.Storage.Models.Base;

/// <summary>
/// Type-safe abstraction of different storage protocol paths.
/// </summary>
public interface IStoragePath
{
    /// <summary>
    /// Appends a key to the current path.
    /// </summary>
    /// <param name="keyName">Name of the key</param>
    /// <returns>A new storage path object</returns>
    IStoragePath Join(string keyName);

    /// <summary>
    /// Relative path to an object in the blob storage
    /// </summary>
    public string ObjectKey { get; }

    /// <summary>
    /// Converts the given path to HDFS path string.
    /// </summary>
    /// <returns>String representing HDFS path information</returns>
    public string ToHdfsPath();
}
