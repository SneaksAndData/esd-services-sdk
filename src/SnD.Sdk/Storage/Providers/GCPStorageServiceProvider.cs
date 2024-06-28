using Microsoft.Extensions.DependencyInjection;
using System;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Storage.Providers.Configurations;

namespace Snd.Sdk.Storage.Providers
{
    /// <summary>
    /// Provider for GCP storage services.
    /// Use this in Startup to configure the app to run on GCP storage layer.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class GCPStorageServiceProvider
    {
        /// <summary>
        /// Adds selected services to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <param name="gcpConfig">GCP Storage configuration.</param>
        /// <returns></returns>
        public static IServiceCollection AddGcpStorage(this IServiceCollection services, GCPStorageConfiguration gcpConfig)
        {
            throw new NotImplementedException();
        }
    }
}
