using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Minio;
using Minio.DataModel;
using SnD.Sdk.Storage.Minio.Base;

namespace SnD.Sdk.Storage.Minio;

/// <summary>
/// Provides an implementation of the <see cref="IMinioService"/> interface for interacting with Minio storage.
/// </summary>
public class MinioService : IMinioService
{
    private readonly IMinioClient minioClient;

    /// <summary>
    /// Initializes a new instance of the <see cref="MinioService"/> class.
    /// </summary>
    /// <param name="minioClient">The Minio client to be used for storage operations.</param>
    public MinioService(IMinioClient minioClient)
    {
        this.minioClient = minioClient;
    }


    /// <summary>
    /// Asynchronously retrieves an object from the Minio storage.
    /// </summary>
    /// <param name="bucketName">The name of the bucket where the object is stored.</param>
    /// <param name="objectName">The name of the object to retrieve.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public Task GetObjectAsync(string bucketName, string objectName)
    {
        GetObjectArgs args = new GetObjectArgs().WithBucket(bucketName).WithObject(objectName);
        return minioClient.GetObjectAsync(args);
    }

    /// <summary>
    /// Asynchronously sets the Redis bucket notification configuration.
    /// </summary>
    /// <param name="bucketName">The name of the bucket to set the notification configuration for.</param>
    /// <param name="redisQueueArn">The Amazon Resource Name (ARN) of the Redis queue where notifications should be sent.</param>
    /// <param name="events">A list of bucket events that should trigger notifications.</param>
    /// <returns>A Task representing the asynchronous operation.</returns>
    public Task SetRedisBucketNotificationAsync(string bucketName, string redisQueueArn, List<BucketEvent> events)
    {
        var notification = new BucketNotification();
        var eventTypes = events.Select(e => e.ToEventType()).ToList();

        var queueConfiguration = new QueueConfig(redisQueueArn);
        queueConfiguration.AddEvents(eventTypes);
        notification.AddQueue(queueConfiguration);

        var args = new SetBucketNotificationsArgs().WithBucket(bucketName)
            .WithBucketNotificationConfiguration(notification);
        return minioClient.SetBucketNotificationsAsync(args);
    }
}
