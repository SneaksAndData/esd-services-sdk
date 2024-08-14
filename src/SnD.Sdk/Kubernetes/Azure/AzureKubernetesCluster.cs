using System;
using Azure.Storage.Files.Shares;
using Microsoft.Extensions.Logging;
using Snd.Sdk.Storage.Azure;
using Snd.Sdk.Storage.Base;

namespace Snd.Sdk.Kubernetes.Azure
{
    /// <summary>
    /// A Kubernetes Cluster with Azure cloud backend (AKS). Only use this class if you use Azure-specific configurations.
    /// </summary>
    public class AzureKubernetesCluster : KubernetesCluster
    {
        /// <summary>
        /// Creates an instance of <see cref="AzureKubernetesCluster"/>.
        /// </summary>
        /// <param name="kubeConfigLocation"></param>
        /// <param name="loggerFactory"></param>
        public AzureKubernetesCluster(string kubeConfigLocation, ILoggerFactory loggerFactory) : base(
            kubeConfigLocation, loggerFactory)
        {
            this.InitSfs(loggerFactory);
        }

        /// <inheritdoc />
        public override ISharedFileSystemService SharedFileSystem()
        {
            return this.sharedFileSystem;
        }

        private void InitSfs(ILoggerFactory loggerFactory)
        {
            var accountName =
                Environment.GetEnvironmentVariable($"PROTEUS__K8S_AZURE_RWMPV_ACCOUNT_NAME") ??
                Environment.GetEnvironmentVariable(
                    $"PROTEUS__K8S_{this.ClusterName.Replace("-", "_").ToUpperInvariant()}_AZURE_RWMPV_ACCOUNT_NAME");

            var accountKey =
                Environment.GetEnvironmentVariable($"PROTEUS__K8S_AZURE_RWMPV_ACCOUNT_KEY") ??
                Environment.GetEnvironmentVariable(
                    $"PROTEUS__K8S_{this.ClusterName.Replace("-", "_").ToUpperInvariant()}_AZURE_RWMPV_ACCOUNT_KEY");

            if (accountKey != null && accountName != null)
            {
                var mountResourceConnectionString =
                    $"DefaultEndpointsProtocol=https;AccountName={accountName};AccountKey={accountKey};EndpointSuffix=core.windows.net";
                this.sharedFileSystem = new AzureSharedFSService(new ShareServiceClient(mountResourceConnectionString),
                    loggerFactory.CreateLogger<AzureSharedFSService>());
                return;
            }

            this.logger.LogWarning(
                "Missing configuration for external access to RMW mount on AKS cluster {clusterName}",
                this.ClusterName);
        }
    }
}
