using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace Snd.Sdk.Storage.Minio.Models
{
    /// <summary>
    /// Represents the identity of a user.
    /// </summary>
    public record UserIdentity
    {
        /// <summary>
        /// Gets or sets the principal ID of the user.
        /// </summary>
        [JsonPropertyName("principalId")]
        public string PrincipalId { get; set; }
    }

    /// <summary>
    /// Represents the parameters of a request.
    /// </summary>
    public record RequestParameters
    {
        /// <summary>
        /// Gets or sets the principal ID of the requester.
        /// </summary>
        [JsonPropertyName("principalId")]
        public string PrincipalId { get; set; }

        /// <summary>
        /// Gets or sets the region of the request.
        /// </summary>
        [JsonPropertyName("region")]
        public string Region { get; set; }

        /// <summary>
        /// Gets or sets the source IP address of the request.
        /// </summary>
        [JsonPropertyName("sourceIPAddress")]
        public string SourceIPAddress { get; set; }
    }

    /// <summary>
    /// Represents a storage bucket.
    /// </summary>
    public record Bucket
    {
        /// <summary>
        /// Gets or sets the name of the bucket.
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; }

        /// <summary>
        /// Gets or sets the owner identity of the bucket.
        /// </summary>
        [JsonPropertyName("ownerIdentity")]
        public UserIdentity OwnerIdentity { get; set; }

        /// <summary>
        /// Gets or sets the Amazon Resource Name (ARN) of the bucket.
        /// </summary>
        [JsonPropertyName("arn")]
        public string Arn { get; set; }
    }

    /// <summary>
    /// Represents an object stored in a bucket.
    /// </summary>
    public record 
    
    {
        /// <summary>
        /// Gets or sets the key of the object.
        /// </summary>
        [JsonPropertyName("key")]
        public string Key { get; set; }

        /// <summary>
        /// Gets or sets the size of the object.
        /// </summary>
        [JsonPropertyName("size")]
        public int Size { get; set; }

        /// <summary>
        /// Gets or sets the ETag of the object.
        /// </summary>
        [JsonPropertyName("eTag")]
        public string ETag { get; set; }

        /// <summary>
        /// Gets or sets the content type of the object.
        /// </summary>
        [JsonPropertyName("contentType")]
        public string ContentType { get; set; }

        /// <summary>
        /// Gets or sets the user metadata of the object.
        /// </summary>
        [JsonPropertyName("userMetadata")]
        public Dictionary<string, string> UserMetadata { get; set; }

        /// <summary>
        /// Gets or sets the sequencer of the object.
        /// </summary>
        [JsonPropertyName("sequencer")]
        public string Sequencer { get; set; }
    }

    /// <summary>
    /// Represents the S3 entity in an event.
    /// </summary>
    public record S3
    {
        /// <summary>
        /// Gets or sets the S3 schema version.
        /// </summary>
        [JsonPropertyName("s3SchemaVersion")]
        public string S3SchemaVersion { get; set; }

        /// <summary>
        /// Gets or sets the configuration ID.
        /// </summary>
        [JsonPropertyName("configurationId")]
        public string ConfigurationId { get; set; }

        /// <summary>
        /// Gets or sets the bucket involved in the event.
        /// </summary>
        [JsonPropertyName("bucket")]
        public Bucket Bucket { get; set; }

        /// <summary>
        /// Gets or sets the object involved in the event.
        /// </summary>
        [JsonPropertyName("object")]
        public Object Object { get; set; }
    }

    /// <summary>
    /// Represents the source of an event.
    /// </summary>
    public record EventSource
    {
        /// <summary>
        /// Gets or sets the host of the event source.
        /// </summary>
        [JsonPropertyName("host")]
        public string Host { get; set; }

        /// <summary>
        /// Gets or sets the port of the event source.
        /// </summary>
        [JsonPropertyName("port")]
        public string Port { get; set; }

        /// <summary>
        /// Gets or sets the user agent of the event source.
        /// </summary>
        [JsonPropertyName("userAgent")]
        public string UserAgent { get; set; }
    }

    /// <summary>
    /// Represents an event in the system.
    /// </summary>
    public record Event
    {
        /// <summary>
        /// Gets or sets the version of the event.
        /// </summary>
        [JsonPropertyName("eventVersion")]
        public string EventVersion { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary>
        [JsonPropertyName("eventSource")]
        public string EventSource { get; set; }

        /// <summary>
        /// Gets or sets the AWS region where the event occurred.
        /// </summary>
        [JsonPropertyName("awsRegion")]
        public string AwsRegion { get; set; }

        /// <summary>
        /// Gets or sets the time when the event occurred.
        /// </summary>
        [JsonPropertyName("eventTime")]
        public string EventTime { get; set; }

        /// <summary>
        /// Gets or sets the name of the event.
        /// </summary>
        [JsonPropertyName("eventName")]
        public string EventName { get; set; }

        /// <summary>
        /// Gets or sets the identity of the user who triggered the event.
        /// </summary>
        [JsonPropertyName("userIdentity")]
        public UserIdentity UserIdentity { get; set; }

        /// <summary>
        /// Gets or sets the parameters of the request that triggered the event.
        /// </summary>
        [JsonPropertyName("requestParameters")]
        public RequestParameters RequestParameters { get; set; }

        /// <summary>
        /// Gets or sets the S3 details of the event.
        /// </summary>
        [JsonPropertyName("s3")]
        public S3 S3Details { get; set; }

        /// <summary>
        /// Gets or sets the source of the event.
        /// </summary
        [JsonPropertyName("source")]
        public EventSource Source { get; set; }
    }

    /// <summary>
    /// Wraps a list of events.
    /// </summary>
    public record EventWrapper
    {
        /// <summary>
        /// Gets or sets the list of events.
        /// </summary>
        public List<Event> Event { get; set; }
        /// <summary>
        /// Gets or sets the time of the event.
        /// </summary>
        public string EventTime { get; set; }
    }
}
