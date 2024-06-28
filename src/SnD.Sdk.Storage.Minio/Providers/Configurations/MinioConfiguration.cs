namespace Snd.Sdk.Storage.Minio.Providers.Configurations;

/// <summary>
/// Configuration settings for Minio Storage.
/// </summary>
public sealed class MinioConfiguration
{
    /// <summary>
    ///  Minio S3 endpoint.
    /// </summary>
    public string Endpoint { get; set; }

    /// <summary>
    /// Access Key
    /// </summary>
    public string AccessKey { get; set; }

    /// <summary>
    ///  Secret Key
    /// </summary>
    public string SecretKey  { get; set; }

    /// <summary>
    ///  Region.
    /// </summary>
    public string Region { get; set; }

    /// <summary>
    /// Enable SSL connectivity.
    /// </summary>
    public bool UseSsl { get; set; }

    /// <summary>
    /// Initialize from environment variables.
    /// </summary>
    /// <returns></returns>
    public static MinioConfiguration CreateFromEnv()
    {
        return new MinioConfiguration
        {
            Endpoint = Environment.GetEnvironmentVariable("MINIO_ENDPOINT") ?? "",
            AccessKey = Environment.GetEnvironmentVariable("MINIO_ACCESS_KEY") ?? "",
            SecretKey =Environment.GetEnvironmentVariable("MINIO_SECRET_KEY") ?? "",
            Region = Environment.GetEnvironmentVariable("MINIO_REGION") ?? "",
            UseSsl = bool.Parse(Environment.GetEnvironmentVariable("MINIO_USE_SSL") ?? "true")
        };
    }
}
