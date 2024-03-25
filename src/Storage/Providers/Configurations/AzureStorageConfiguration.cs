using System;
using System.Diagnostics.CodeAnalysis;

namespace Snd.Sdk.Storage.Providers.Configurations
{
    /// <summary>
    /// Configuration for Azure Storage.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class AzureStorageConfiguration
    {
        /// <summary>
        /// Storage account connection string.
        /// </summary>
        public string StorageAccountConnectionString { get; set; }

        /// <summary>
        /// Exponential backoff max delay betwee retries.
        /// </summary>
        public TimeSpan BackOffDelay { get; set; }

        /// <summary>
        /// Max retries for API operations.
        /// </summary>
        public int MaxRetries { get; set; }

        /// <summary>
        /// Initialize from environment variables.
        /// </summary>
        /// <returns></returns>
        public static AzureStorageConfiguration CreateFromEnv()
        {
            return new AzureStorageConfiguration
            {
                StorageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION"),
                BackOffDelay = TimeSpan.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_BACKOFF_DELAY")),
                MaxRetries = int.Parse(Environment.GetEnvironmentVariable("AZURE_STORAGE_MAXRETRIES"))
            };
        }

        /// <summary>
        /// Use default retry settings with connection string from environment variable.
        /// </summary>
        /// <returns></returns>
        public static AzureStorageConfiguration CreateDefault()
        {
            return new AzureStorageConfiguration
            {
                StorageAccountConnectionString = Environment.GetEnvironmentVariable("AZURE_STORAGE_CONNECTION"),
                BackOffDelay = TimeSpan.FromSeconds(1),
                MaxRetries = 10
            };
        }
    }
}
