using Snd.Sdk.Kubernetes.Config;

namespace Snd.Sdk.Kubernetes.Base
{
    /// <summary>
    /// Configurator class for the kube fleet.
    /// </summary>
    public interface IKubeFleetBuilder
    {
        /// <summary>
        /// Provisions AKS-specific resources on each kube fleet member, if they are configured.
        /// </summary>
        /// <returns></returns>
        IKubeFleetBuilder OnAks();

        /// <summary>
        /// Provisions a regular <see cref="KubernetesCluster"/> class for each kube fleet member.
        /// </summary>
        /// <returns></returns>
        IKubeFleetBuilder OnAny();

        /// <summary>
        /// Applies all configurations and produces a configured fleet.
        /// </summary>
        /// <returns></returns>
        IKubeFleet Build();
    }
}
