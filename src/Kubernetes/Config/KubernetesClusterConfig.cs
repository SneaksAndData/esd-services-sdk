using System.Collections.Generic;

namespace Snd.Sdk.Kubernetes.Config
{
    /// <summary>
    /// Represents a single member of a kubefleet connected to an app.
    /// </summary>
    public class KubeFleetMemberConfig
    {
        /// <summary>
        /// Unique name for this member.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Location of a kube config for the cluster associated with this member.
        /// </summary>
        public string KubeConfigLocation { get; set; }

        /// <summary>
        /// Namespace in the target cluster where workloads will be created.
        /// </summary>
        public string Namespace { get; set; }

        /// <summary>
        /// Custom tags assigned to this fleet.
        /// </summary>
        public Dictionary<string, string> Tags { get; set; }

        /// <summary>
        /// Indicates whether this member can be used for scheduling workloads.
        /// </summary>
        public bool IsActive { get; set; }
    }
}
