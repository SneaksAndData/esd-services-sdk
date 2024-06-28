using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Akka;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Base.Streaming;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Binary object storage abstraction.
/// </summary>
public interface IBlobStorageService : IBlobStorageReader,
    IBlobStorageWriter,
    IBlobStreamWriter
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
    /// Deletes a blob.
    /// </summary>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="blobName">Blob name.</param>
    /// <returns></returns>
    Task<bool> RemoveBlob(string blobPath, string blobName);

    /// <summary>
    /// Moves a blob from one container to another, in the same storage account.
    /// </summary>
    /// <param name="sourcePath">Path to a source blob.</param>
    /// <param name="sourceName">Name of a source blob.</param>
    /// <param name="targetPath">Path to a target blob.</param>
    /// <param name="targetBlobName">Name of a target blob.</param>
    /// <returns></returns>
    Task<bool> MoveBlob(string sourcePath, string sourceName, string targetPath, string targetBlobName);

    /// <summary>
    /// Generates an pre-authenticated URL that can be used to read the blob over HTTP protocol.
    /// </summary>
    /// <param name="blobPath">Path to the blob.</param>
    /// <param name="blobName">Name of the blob.</param>
    /// <param name="kwOptions">Additional key-value arguments for URI generator.</param>
    /// <returns></returns>
    Uri GetBlobUri(string blobPath, string blobName, params ValueTuple<string, object>[] kwOptions);

    /// <summary>
    /// Stream blob item metadata for blobs under provided path.
    /// </summary>
    /// <param name="blobPath">Path to list.</param>
    /// <returns></returns>
    Source<StoredBlob, NotUsed> ListBlobs(string blobPath);

    /// <summary>
    /// Retrieve blob item metadata for blobs under provided path, as an enumerable.
    /// </summary>
    /// <param name="blobPath">Path to list.</param>
    /// <returns></returns>
    IEnumerable<StoredBlob> ListBlobsAsEnumerable(string blobPath);
}
