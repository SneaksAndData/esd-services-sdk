using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Blobs;
using Azure.Storage.Files.Shares;
using Azure.Storage.Queues;
using Microsoft.Extensions.DependencyInjection;
using System.Diagnostics.CodeAnalysis;
using Akka.Actor;
using Akka.Hosting;
using Akka.Streams;
using Snd.Sdk.Storage.Models.BlobPath;
using Snd.Sdk.Storage.Azure;
using Snd.Sdk.Storage.Base;
using Snd.Sdk.Storage.Providers.Configurations;

namespace Snd.Sdk.Storage.Providers
{
    /// <summary>
    /// Provider for Azure storage services.
    /// Use this in Startup to configure the app to run on Azure storage layer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class AzureStorageServiceProvider
    {
        /// <summary>
        /// Adds Azure Table client to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="azConfig">Azure Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddAzureTable<T>(this IServiceCollection services, AzureStorageConfiguration azConfig) where T : class, ITableEntity, new()
        {
            services.AddSingleton(typeof(TableServiceClient), provider =>
            {
                var clientOptions = new TableClientOptions();
                clientOptions.Retry.Mode = RetryMode.Exponential;
                clientOptions.Retry.Delay = azConfig.BackOffDelay;
                clientOptions.Retry.MaxRetries = azConfig.MaxRetries;

                return new TableServiceClient(azConfig.StorageAccountConnectionString, clientOptions);
            });

            return services.AddSingleton<IEntityCollectionService<T>, AzureTableService<T>>();
        }

        /// <summary>
        /// Adds Azure Queue client to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="azConfig">Azure Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddAzureQueue(this IServiceCollection services, AzureStorageConfiguration azConfig)
        {
            services.AddSingleton(typeof(QueueServiceClient), provider =>
            {
                var clientOptions = new QueueClientOptions();
                clientOptions.Retry.Mode = RetryMode.Exponential;
                clientOptions.Retry.Delay = azConfig.BackOffDelay;
                clientOptions.Retry.MaxRetries = azConfig.MaxRetries;

                return new QueueServiceClient(azConfig.StorageAccountConnectionString, clientOptions);
            });

            return services.AddSingleton<IQueueService, AzureQueueService>();
        }

        /// <summary>
        /// Adds Azure Blob Storage client to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="azConfig">Azure Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddAzureBlob(this IServiceCollection services, AzureStorageConfiguration azConfig)
        {
            services.AddSingleton(typeof(BlobServiceClient), provider =>
            {
                var clientOptions = new BlobClientOptions();
                clientOptions.Retry.Mode = RetryMode.Exponential;
                clientOptions.Retry.Delay = azConfig.BackOffDelay;
                clientOptions.Retry.MaxRetries = azConfig.MaxRetries;

                return new BlobServiceClient(azConfig.StorageAccountConnectionString, clientOptions);
            });

            return services.AddSingleton<IBlobStorageService, AzureBlobStorageService>();
        }

        /// <summary>
        /// Adds Azure File Share Storage client to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="azConfig">Azure Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddAzureSharedFileSystem(this IServiceCollection services, AzureStorageConfiguration azConfig)
        {
            services.AddSingleton(typeof(ShareServiceClient), provider =>
            {
                var clientOptions = new ShareClientOptions();
                clientOptions.Retry.Mode = RetryMode.Exponential;
                clientOptions.Retry.Delay = azConfig.BackOffDelay;
                clientOptions.Retry.MaxRetries = azConfig.MaxRetries;

                return new ShareServiceClient(azConfig.StorageAccountConnectionString, clientOptions);
            });

            return services.AddSingleton<ISharedFileSystemService, AzureSharedFSService>();
        }
    }
}
