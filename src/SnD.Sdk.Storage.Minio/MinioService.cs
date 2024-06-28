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
        BucketNotification notification = new BucketNotification();
        List<EventType> eventTypes = events.Select(e => e.ToEventType()).ToList();

        QueueConfig queueConfiguration = new QueueConfig(redisQueueArn);
        queueConfiguration.AddEvents(eventTypes);
        notification.AddQueue(queueConfiguration);

        SetBucketNotificationsArgs args = new SetBucketNotificationsArgs().WithBucket(bucketName)
            .WithBucketNotificationConfiguration(notification);
        return minioClient.SetBucketNotificationsAsync(args);
    }
}
