using Akka;
using Akka.IO;
using Akka.Streams.Dsl;

namespace Snd.Sdk.Storage.Base.Streaming;

/// <summary>
/// Akka-based streaming writer for blob storage.
/// </summary>
public interface IBlobStreamWriter
{
    /// <summary>
    /// Writes incoming stream to the specified blob and returns operation result downstream.
    /// </summary>
    /// <param name="blobPath"></param>
    /// <param name="blobName"></param>
    /// <returns></returns>
    Flow<ByteString, bool, NotUsed> StreamToBlob(string blobPath, string blobName);
}
