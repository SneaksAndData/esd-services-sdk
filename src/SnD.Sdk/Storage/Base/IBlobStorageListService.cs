using System.Collections.Generic;
using Akka;
using Akka.Streams.Dsl;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Binary object storage listing abstraction.
/// </summary>
public interface IBlobStorageListService
{
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
