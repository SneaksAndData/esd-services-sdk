using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Kubernetes.Azure;
using Snd.Sdk.Kubernetes.Base;

namespace Snd.Sdk.Kubernetes
{
    /// <summary>
    /// Generic implementation on <see cref="IKubeFleetBuilder"/>.
    /// </summary>
    public sealed class KubeFleetBuilder : IKubeFleetBuilder
    {
        private readonly KubeFleet fleet;
        private readonly string kubeConfigLocation;
        private readonly ILoggerFactory loggerFactory;

        /// <summary>
        /// Creates an new instance of <see cref="KubeFleetBuilder"/>.
        /// </summary>
        /// <param name="kubeConfigLocation"></param>
        /// <param name="loggerFactory"></param>
        private KubeFleetBuilder(string kubeConfigLocation, ILoggerFactory loggerFactory)
        {
            this.fleet = new KubeFleet();
            this.loggerFactory = loggerFactory;
            this.kubeConfigLocation = kubeConfigLocation;
        }

        /// <summary>
        /// Creates a configurator instance from fleet config.
        /// </summary>
        /// <param name="loggerFactory"></param>
        /// <returns></returns>
        public static IKubeFleetBuilder Create(ILoggerFactory loggerFactory)
        {
            return new KubeFleetBuilder(Environment.GetEnvironmentVariable("PROTEUS_KUBERNETES_CONFIG_LOCATION"), loggerFactory);
        }

        /// <inheritdoc />
        public IKubeFleetBuilder OnAny()
        {
            foreach (var kubeconfigFile in Directory.GetFiles(this.kubeConfigLocation).Where(fileName => fileName.EndsWith(".kubeconfig")))
            {
                this.fleet.AddMember(new KubernetesCluster(kubeconfigFile, this.loggerFactory));
            }

            return this;
        }

        /// <inheritdoc />
        public IKubeFleetBuilder OnAks()
        {
            foreach (var kubeconfigFile in Directory.GetFiles(this.kubeConfigLocation).Where(fileName => fileName.EndsWith(".kubeconfig")))
            {
                this.fleet.AddMember(new AzureKubernetesCluster(kubeconfigFile, this.loggerFactory));
            }

            return this;
        }

        /// <inheritdoc />
        public IKubeFleet Build()
        {
            return this.fleet;
        }
    }
}
