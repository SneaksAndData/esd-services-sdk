using Minio.DataModel;

namespace SnD.Sdk.Storage.Minio;

/// <summary>
/// Represents a bucket event.
/// </summary>
public class BucketEvent
{
    /// <summary>
    /// Represents a bucket event in the Minio storage system.
    /// </summary>
    public static readonly BucketEvent ObjectCreatedAll = new("s3:ObjectCreated:*");
    /// <summary>
    /// Event for object creation via PUT.
    /// </summary>
    public static readonly BucketEvent ObjectCreatedPut = new("s3:ObjectCreated:Put");
    /// <summary>
    /// Event for object creation via POST.
    /// </summary>
    public static readonly BucketEvent ObjectCreatedPost = new("s3:ObjectCreated:Post");
    /// <summary>
    /// Event for object creation via COPY.
    /// </summary>
    public static readonly BucketEvent ObjectCreatedCopy = new("s3:ObjectCreated:Copy");

    /// <summary>
    /// Event for object creation via multipart upload completion.
    /// </summary>
    public static readonly BucketEvent ObjectCreatedCompleteMultipartUpload =
        new("s3:ObjectCreated:CompleteMultipartUpload");

    /// <summary>
    /// Event for object access via GET.
    /// </summary>
    public static readonly BucketEvent ObjectAccessedGet = new("s3:ObjectAccessed:Get");
    /// <summary>
    /// Event for object access via HEAD.
    /// </summary>
    public static readonly BucketEvent ObjectAccessedHead = new("s3:ObjectAccessed:Head");
    /// <summary>
    /// Event for all object access.
    /// </summary>
    public static readonly BucketEvent ObjectAccessedAll = new("s3:ObjectAccessed:*");
    /// <summary>
    /// Event for all object removal.
    /// </summary>
    public static readonly BucketEvent ObjectRemovedAll = new("s3:ObjectRemoved:*");
    /// <summary>
    /// Event for object removal via DELETE.
    /// </summary>
    public static readonly BucketEvent ObjectRemovedDelete = new("s3:ObjectRemoved:Delete");
    /// <summary>
    /// Event for creation of a delete marker.
    /// </summary>
    public static readonly BucketEvent ObjectRemovedDeleteMarkerCreated = new("s3:ObjectRemoved:DeleteMarkerCreated");
    /// <summary>
    /// Event for loss of an object with reduced redundancy.
    /// </summary>
    public static readonly BucketEvent ReducedRedundancyLostObject = new("s3:ReducedRedundancyLostObject");

    /// <summary>
    /// The value of the bucket event.
    /// </summary>
    public string value;

    /// <summary>
    /// Initializes a new instance of the <see cref="BucketEvent"/> class.
    /// </summary>
    /// <param name="value">The value of the bucket event.</param>
    public BucketEvent(string value)
    {
        this.value = value;
    }

    /// <summary>
    /// Transforms the current <see cref="BucketEvent"/> instance into its corresponding EventType representation as defined in the Minio SDK.
    /// </summary>
    /// <returns>An EventType object that mirrors the value of the current <see cref="BucketEvent"/> instance.</returns>
    public EventType ToEventType()
    {
        return new EventType(this.value);
    }
}
