using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Amazon.S3;
using Amazon.SQS;
using Snd.Sdk.Storage.Amazon;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Models;
using Snd.Sdk.Storage.Models.BlobPath;
using Snd.Sdk.Storage.Providers.Configurations;

namespace Snd.Sdk.Storage.Providers
{
    /// <summary>
    /// Provider for AWS storage services.
    /// Use this in Startup to configure the app to run on AWS storage layer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AmazonStorageServiceProvider
    {
        /// <summary>
        /// Adds selected services to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="awsConfig">AWS Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddAwsS3Writer(this IServiceCollection services,
            AmazonStorageConfiguration awsConfig)
        {
            var clientConfig = new AmazonS3Config
            {
                UseHttp = awsConfig.UseHttp,
                ForcePathStyle = true,
                ServiceURL = awsConfig.ServiceUrl.ToString(),
                UseAccelerateEndpoint = false,
                AuthenticationRegion = awsConfig.AuthenticationRegion,
                SignatureVersion = awsConfig.SignatureVersion
            };

            services.AddSingleton<IAmazonS3>(new AmazonS3Client(awsConfig.AccessKey, awsConfig.SecretKey, clientConfig));
            return services.AddSingleton<IBlobStorageWriter<AmazonS3StoragePath>, AmazonBlobStorageService>();
        }

        /// <summary>
        /// Adds Amazon SQS services to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="awsConfig">AWS Storage configuration.</param>
        /// <returns>The updated service collection.</returns>
        public static IServiceCollection AddAwsSqs(this IServiceCollection services,
            AmazonStorageConfiguration awsConfig)
        {
            var clientConfig = new AmazonSQSConfig
            {
                UseHttp = awsConfig.UseHttp,
                ServiceURL = awsConfig.ServiceUrl.ToString(),
                AuthenticationRegion = awsConfig.AuthenticationRegion,
                SignatureVersion = awsConfig.SignatureVersion
            };
            services.AddSingleton<IAmazonSQS>(new AmazonSQSClient(awsConfig.AccessKey, awsConfig.SecretKey, clientConfig));
            return services.AddSingleton<IQueueService<AmazonSqsSendResponse, AmazonSqsReleaseResponse>, AmazonSqsService>();
        }
    }
}
