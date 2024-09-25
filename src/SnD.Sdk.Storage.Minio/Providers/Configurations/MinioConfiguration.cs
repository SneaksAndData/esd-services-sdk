using System;
using SnD.Sdk.Extensions.Environment.Hosting;

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
    public string SecretKey { get; set; }

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
        string endpoint = EnvironmentExtensions.GetDomainEnvironmentVariable("MINIO_ENDPOINT");
        bool isUri = Uri.TryCreate(endpoint, UriKind.Absolute, out Uri uriResult) &&
                     (uriResult.Scheme == Uri.UriSchemeHttp || uriResult.Scheme == Uri.UriSchemeHttps);

        string minioUseSslValue = EnvironmentExtensions.GetDomainEnvironmentVariable("MINIO_USE_SSL");
        bool useSsl = !bool.TryParse(minioUseSslValue, out useSsl) || useSsl;

        return new MinioConfiguration
        {
            Endpoint = isUri ? uriResult.Host : endpoint,
            AccessKey = EnvironmentExtensions.GetDomainEnvironmentVariable("MINIO_ACCESS_KEY"),
            SecretKey = EnvironmentExtensions.GetDomainEnvironmentVariable("MINIO_SECRET_KEY"),
            Region = EnvironmentExtensions.GetDomainEnvironmentVariable("MINIO_REGION"),
            UseSsl = useSsl
        };
    }
}
