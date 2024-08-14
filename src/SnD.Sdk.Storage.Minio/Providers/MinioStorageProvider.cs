using Microsoft.Extensions.DependencyInjection;
using Minio;
using SnD.Sdk.Storage.Minio;
using SnD.Sdk.Storage.Minio.Base;
using Snd.Sdk.Storage.Minio.Providers.Configurations;

namespace Snd.Sdk.Storage.Minio.Providers;

/// <summary>
/// Provides extension methods to the <see cref="IServiceCollection"/> for configuring Minio storage services.
/// </summary>
public static class MinioStorageProvider
{
    /// <summary>
    /// Adds and configures the Minio storage services.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add the services to.</param>
    /// <param name="minioConfiguration">The configuration for the Minio storage services.</param>
    /// <returns>The same service collection so that multiple calls can be chained.</returns>
    public static IServiceCollection AddMinioStorage(this IServiceCollection services,
        MinioConfiguration minioConfiguration)
    {
        services.AddSingleton<IMinioClient>(sp =>
            new MinioClient()
                .WithEndpoint(minioConfiguration.Endpoint)
                .WithCredentials(minioConfiguration.AccessKey, minioConfiguration.SecretKey)
                .WithSSL(minioConfiguration.UseSsl)
                .WithRegion(minioConfiguration.Region)
                .Build());

        return services.AddSingleton<IMinioService, MinioService>();
    }
}
