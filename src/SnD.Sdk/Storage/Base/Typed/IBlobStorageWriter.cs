using System;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Storage.Models.Base;

namespace SnD.Sdk.Storage.Base.Typed;

/// <summary>
/// Binary object storage write-only abstraction, typed by path.
/// <typeparam name="TPath">Type of the path object. Must implement IStoragePath.</typeparam>
/// </summary>
public interface ITypedBlobStorageWriter<in TPath> where TPath : IStoragePath
{
    /// <summary>
    /// Saves byte array to a blob.
    /// </summary>
    /// <param name="bytes">Bytes to save.</param>
    /// <param name="blobPath">Blob path.</param>
    /// <param name="overwrite">Whether to overwrite a blob or fail if it exists.</param> 
    /// <returns></returns>
    Task<UploadedBlob> SaveBytesAsBlob(BinaryData bytes, TPath blobPath, bool overwrite = false);

    /// <summary>
    /// Deletes a blob.
    /// </summary>
    /// <param name="blobPath">Blob path.</param>
    /// <returns></returns>
    Task<bool> RemoveBlob(TPath blobPath);
}
