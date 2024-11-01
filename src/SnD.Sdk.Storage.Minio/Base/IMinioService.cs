using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Minio.DataModel;

namespace SnD.Sdk.Storage.Minio.Base;

/// <summary>
/// Interface for Minio service operations.
/// </summary>
public interface IMinioService
{
    /// <summary>
    /// Downloads an object from the Minio storage.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    Task<ObjectStat> GetObjectAsync(string bucketName, string objectName);

    /// <summary>
    /// Asynchronously reads an object from a specified bucket and returns its content as a <see cref="Stream"/>.
    /// </summary>
    /// <param name="bucketName">The name of the bucket where the object is stored.</param>
    /// <param name="objectName">The name of the object to read.</param>
    /// <param name="cancellationToken">An optional cancellation token to cancel the operation.</param>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation, which upon completion returns a <see cref="Stream"/> containing the object's content.</returns>
    Task<Stream> ReadObjectAsync(string bucketName, string objectName, CancellationToken cancellationToken=default);

    /// <summary>
    ///  Sets notification configuration for a given bucket
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="redisQueueArn"></param>
    /// <param name="events"></param>
    /// <returns></returns>
    Task SetRedisBucketNotificationAsync(string bucketName, string redisQueueArn, List<BucketEvent> events);
}

