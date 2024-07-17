using System;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Binary object storage write-only abstraction.
/// </summary>
public interface IBlobStorageWriter
{
    /// <summary>
    /// Saves byte array to a blob.
    /// </summary>
    /// <param name="bytes">Bytes to save.</param>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="blobName">Blob name.</param>
    /// <param name="overwrite">Whether to overwrite a blob or fail if it exists.</param> 
    /// <returns></returns>
    Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, string blobPath, string blobName, bool overwrite = false);

    /// <summary>
    /// Saves string data of a moderate size to a blob.
    /// </summary>
    /// <param name="text">Blob contents.</param>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="blobName">Blob name.</param>
    /// <returns></returns>
    Task<UploadedBlob> SaveTextAsBlob(string text, string blobPath, string blobName);

    /// <summary>
    /// Deletes a blob.
    /// </summary>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="blobName">Blob name.</param>
    /// <returns></returns>
    Task<bool> RemoveBlob(string blobPath, string blobName);

    /// <summary>
    /// Generates a pre-signed URL that can be used to read the blob over HTTP(S) protocol.
    /// </summary>
    /// <param name="blobPath">Path to the blob.</param>
    /// <param name="blobName">Name of the blob.</param>
    /// <param name="kwOptions">Additional key-value arguments for URI generator.</param>
    /// <returns></returns>
    Uri GetBlobUri(string blobPath, string blobName, params ValueTuple<string, object>[] kwOptions);
}
