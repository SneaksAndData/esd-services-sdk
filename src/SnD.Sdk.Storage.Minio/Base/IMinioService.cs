namespace SnD.Sdk.Storage.Minio.Base;

public interface IMinioService
{
    /// <summary>
    /// Downloads an object from the Minio storage.
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="objectName"></param>
    /// <returns></returns>
    Task GetObjectAsync(string bucketName, string objectName);

    /// <summary>
    ///  Sets notification configuration for a given bucket
    /// </summary>
    /// <param name="bucketName"></param>
    /// <param name="redisQueueArn"></param>
    /// <param name="events"></param>
    /// <returns></returns>
    Task SetRedisBucketNotificationAsync(string bucketName, string redisQueueArn, List<BucketEvent> events);
}

