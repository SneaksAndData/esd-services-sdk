using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using SnD.Sdk.Storage.Minio.Base;
using Snd.Sdk.Storage.Minio.Providers.Configurations;

namespace Snd.Sdk.Storage.Minio.Providers;

public static class MinioStorageProvider
{
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

        return services.AddSingleton<IMinioService, IMinioService>();
    }
}
