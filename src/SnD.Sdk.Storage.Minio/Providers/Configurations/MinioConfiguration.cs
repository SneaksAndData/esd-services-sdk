namespace Snd.Sdk.Storage.Minio.Providers.Configurations;

/// <summary>
/// Configuration settings for Minio Storage.
/// </summary>
public class MinioConfiguration
{
    /// <summary>
    ///  Minio S3 endpoint.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Access Key
    /// </summary>
    public string AccessKey => Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "";

    /// <summary>
    ///  Secret Key
    /// </summary>
    public string SecretKey => Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "";

    /// <summary>
    ///  Region.
    /// </summary>
    public string Region { get; set; }

    /// <summary>
    /// Enable SSL connectivity.
    /// </summary>
    public bool UseSsl { get; set; }
}
