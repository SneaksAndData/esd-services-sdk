using Minio;
using Minio.DataModel;
using SnD.Sdk.Storage.Minio.Base;

namespace SnD.Sdk.Storage.Minio;

public class MinioService : IMinioService
{
    private readonly IMinioClient minioClient;

    public MinioService(IMinioClient minioClient)
    {
        this.minioClient = minioClient;
    }

    public Task GetObjectAsync(string bucketName, string objectName)
    {
        GetObjectArgs args = new GetObjectArgs().WithBucket(bucketName).WithObject(objectName);
        return minioClient.GetObjectAsync(args);
    }

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
