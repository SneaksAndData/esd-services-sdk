﻿using System;
using System.IO;
using System.Threading.Tasks;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Base;

/// <summary>
/// Read-only binary object storage abstraction.
/// </summary>
public interface IBlobStorageReader<in TBlobPath> where TBlobPath : IStoragePath
{

    /// <summary>
    /// Reads blob content as type T, using provided deserializer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="blobPath">Provider-specific blob path.</param>
    /// <param name="deserializer">Function to deserialize blob content with.</param>
    /// <returns></returns>
    T GetBlobContent<T>(TBlobPath blobPath, Func<BinaryData, T> deserializer);

    /// <summary>
    /// A task that reads blob content as type T, using provided deserializer.
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="blobPath">Provider-specific blob path.</param>
    /// <param name="deserializer">Function to deserialize blob content with.</param>
    /// <returns></returns>
    Task<T> GetBlobContentAsync<T>(TBlobPath blobPath, Func<BinaryData, T> deserializer);

    /// <summary>
    /// Streams a blob to the client, only downloading parts requested by the stream reader.
    /// </summary>
    /// <param name="blobPath">Provider-specific blob path.</param>
    /// <param name="blobName">Name of a blob.</param>
    /// <returns>A readable bytestream.</returns>
    Stream StreamBlobContent(string blobPath, string blobName);
}
