using System;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models.Base;

namespace SnD.Sdk.Storage.Base.Typed;

/// <summary>
/// Read-only binary object storage abstraction, typed by path.
/// <typeparam name="TPath">Type of the path object. Must implement IStoragePath.</typeparam>
/// </summary>
public interface ITypedBlobStorageReader<in TPath> where TPath : IStoragePath
{
    /// <summary>
    /// A task that reads blob content as type T, using provided deserializer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="blobPath">Provider-specific blob path.</param>
    /// <param name="deserializer">Function to deserialize blob content with.</param>
    /// <returns></returns>
    Task<T> GetBlobContentAsync<T>(TPath blobPath, Func<BinaryData, T> deserializer);
}
