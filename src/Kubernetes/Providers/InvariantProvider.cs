using k8s;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System;
using System.Diagnostics.CodeAnalysis;
using Snd.Sdk.Kubernetes.Base;
using Snd.Sdk.Kubernetes.Exceptions;

namespace Snd.Sdk.Kubernetes.Providers
{
    /// <summary>
    /// Provider for Kubernetes service, invariant to the hosting platform.
    /// Use this in Startup to configure the app to run on a Kubernetes cluster.
    /// NB. This method is only for apps that load cluster configuration on startup.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public static class InvariantProvider
    {
        /// <summary>
        /// Environment variable name for kube config path.
        /// </summary>
        public const string PROTEUS_KUBERNETES_CONFIG_PATH = "PROTEUS_KUBERNETES_CONFIG_PATH";

        /// <summary>
        /// Reads a single-cluster kubernetes configuration, if one is available.
        /// </summary>
        /// <returns></returns>
        /// <exception cref="InvalidStartupConfigurationException"></exception>
        private static KubernetesClientConfiguration GetK8SConfiguration()
        {
            return (KubernetesClientConfiguration.IsInCluster(),
                    string.IsNullOrEmpty(Environment.GetEnvironmentVariable(PROTEUS_KUBERNETES_CONFIG_PATH))) switch
            {
                (true, _) => KubernetesClientConfiguration.InClusterConfig(),
                (false, false) => KubernetesClientConfiguration.BuildConfigFromConfigFile(
                    kubeconfigPath: Environment.GetEnvironmentVariable(PROTEUS_KUBERNETES_CONFIG_PATH)),
                _ => throw new InvalidStartupConfigurationException(
                    $"Application is neither running in-cluster nor has {nameof(PROTEUS_KUBERNETES_CONFIG_PATH)} been provided.")
            };
        }

        /// <summary>
        /// Adds a single Kubernetes client to the DI container.
        /// </summary>
        /// <param name="services">Service collection (DI container).</param>
        /// <returns></returns>
        public static IServiceCollection AddKubernetes(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IKubeCluster), provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();
                var conf = GetK8SConfiguration();
                return KubernetesCluster.CreateFromApi(conf.CurrentContext, new k8s.Kubernetes(conf),
                    loggerFactory);
            });

            return services;
        }

        /// <summary>
        /// Adds Kubernetes Fleet object to the DI container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddKubernetesFleet(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IKubeFleet), provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                return KubeFleetBuilder
                    .Create(loggerFactory)
                    .OnAny()
                    .Build();
            });

            return services;
        }

        /// <summary>
        /// Adds Azure Kubernetes Fleet object to the DI container.
        /// </summary>
        /// <param name="services"></param>
        /// <returns></returns>
        public static IServiceCollection AddAzureKubernetesFleet(this IServiceCollection services)
        {
            services.AddSingleton(typeof(IKubeFleet), provider =>
            {
                var loggerFactory = provider.GetRequiredService<ILoggerFactory>();

                return KubeFleetBuilder
                    .Create(loggerFactory)
                    .OnAks()
                    .Build();
            });

            return services;
        }
    }
}
