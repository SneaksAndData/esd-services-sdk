using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using Snd.Sdk.Kubernetes.Base;

namespace Snd.Sdk.Kubernetes
{
    /// <summary>
    /// Generic Kube fleet implementation.
    /// </summary>
    public class KubeFleet : IKubeFleet
    {
        private ImmutableList<IKubeCluster> members = ImmutableList<IKubeCluster>.Empty;

        /// <summary>
        /// Creates an instance of <see cref="KubeFleet"/>.
        /// </summary>
        /// <returns></returns>
        public KubeFleet() { }

        /// <inheritdoc />
        public void AddMember(IKubeCluster member)
        {
            if (this.members.Find(m => member.KubeApi.BaseUri.ToString() == m.KubeApi.BaseUri.ToString()) == null)
            {
                this.members = this.members.Add(member);
            }
        }

        /// <inheritdoc />
        public ImmutableList<IKubeCluster> GetAllMembers() => this.members;

        /// <inheritdoc/>
        public IKubeCluster GetMemberByName(string name)
        {
            return members.Find(m => m.ClusterName.Equals(name, System.StringComparison.InvariantCultureIgnoreCase));
        }
    }
}
