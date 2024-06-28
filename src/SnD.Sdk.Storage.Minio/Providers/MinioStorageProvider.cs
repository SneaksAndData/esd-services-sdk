using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Minio;
using SnD.Sdk.Storage.Minio.Base;
using Snd.Sdk.Storage.Minio.Providers.Configurations;

namespace Snd.Sdk.Storage.Minio.Providers;

public static class MinioStorageProvider
{

    public static IServiceCollection AddMinioStorage(this IServiceCollection services, IConfiguration appConfiguration)
    {

        var minioConfiguration = new MinioConfiguration();
        appConfiguration.GetSection(nameof(MinioStorageProvider)).Bind(minioConfiguration);

        var minio = new MinioClient()
            .WithEndpoint(minioConfiguration.Endpoint)
            .WithCredentials(minioConfiguration.AccessKey, minioConfiguration.SecretKey)
             .WithSSL(minioConfiguration.UseSsl)
            .WithRegion(minioConfiguration.Region)
            .Build();

        services.AddSingleton<IMinioClient>(sp => minio);

        return services.AddSingleton<IMinioService, IMinioService>();

    }

}
