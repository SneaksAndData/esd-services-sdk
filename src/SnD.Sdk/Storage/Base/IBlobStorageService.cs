using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Base.Streaming;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Binary object storage abstraction.
/// </summary>
public interface IBlobStorageService<in TBlobPath> : IBlobStorageReader<TBlobPath>,
    IBlobStorageWriter<TBlobPath>,
    IBlobStreamWriter,
    IBlobStorageListService where TBlobPath : IStoragePath
{
    /// <summary>
    /// Reads blob metadata for the specified blob.
    /// </summary>
    /// <param name="blobPath">Name of a blob container.</param>
    /// <param name="blobName">Name of a blob.</param>
    /// <returns></returns>
    IDictionary<string, string> GetBlobMetadata(string blobPath, string blobName);

    /// <summary>
    /// A task that reads blob metadata for the specified blob.
    /// </summary>
    /// <param name="blobPath">Name of a blob container.</param>
    /// <param name="blobName">Name of a blob.</param>
    /// <returns></returns>        
    Task<IDictionary<string, string>> GetBlobMetadataAsync(string blobPath, string blobName);

    /// <summary>
    /// Moves a blob from one container to another, in the same storage account.
    /// </summary>
    /// <param name="sourcePath">Path to a source blob.</param>
    /// <param name="sourceName">Name of a source blob.</param>
    /// <param name="targetPath">Path to a target blob.</param>
    /// <param name="targetBlobName">Name of a target blob.</param>
    /// <returns></returns>
    Task<bool> MoveBlob(string sourcePath, string sourceName, string targetPath, string targetBlobName);
}
