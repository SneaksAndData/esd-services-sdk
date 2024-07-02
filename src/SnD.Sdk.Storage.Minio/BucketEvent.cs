using Minio.DataModel;

namespace SnD.Sdk.Storage.Minio;

public class BucketEvent
{
    public static readonly BucketEvent ObjectCreatedAll = new("s3:ObjectCreated:*");
    public static readonly BucketEvent ObjectCreatedPut = new("s3:ObjectCreated:Put");
    public static readonly BucketEvent ObjectCreatedPost = new("s3:ObjectCreated:Post");
    public static readonly BucketEvent ObjectCreatedCopy = new("s3:ObjectCreated:Copy");

    public static readonly BucketEvent ObjectCreatedCompleteMultipartUpload =
        new("s3:ObjectCreated:CompleteMultipartUpload");

    public static readonly BucketEvent ObjectAccessedGet = new("s3:ObjectAccessed:Get");
    public static readonly BucketEvent ObjectAccessedHead = new("s3:ObjectAccessed:Head");
    public static readonly BucketEvent ObjectAccessedAll = new("s3:ObjectAccessed:*");
    public static readonly BucketEvent ObjectRemovedAll = new("s3:ObjectRemoved:*");
    public static readonly BucketEvent ObjectRemovedDelete = new("s3:ObjectRemoved:Delete");
    public static readonly BucketEvent ObjectRemovedDeleteMarkerCreated = new("s3:ObjectRemoved:DeleteMarkerCreated");
    public static readonly BucketEvent ReducedRedundancyLostObject = new("s3:ReducedRedundancyLostObject");

    public string value;

    public BucketEvent(string value)
    {
        this.value = value;
    }

    public EventType ToEventType()
    {
        return new EventType(this.value);
    }
}
