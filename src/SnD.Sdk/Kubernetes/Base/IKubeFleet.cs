using System.Collections.Immutable;

namespace Snd.Sdk.Kubernetes.Base
{
    /// <summary>
    /// An array of kubernetes clusters.
    /// </summary>
    public interface IKubeFleet
    {
        /// <summary>
        /// Seaches for a fleet member with a specific name.
        /// </summary>
        /// <param name="name">Name of a desired member.</param>
        /// <returns></returns>
        IKubeCluster GetMemberByName(string name);

        /// <summary>
        /// Returns all members of this fleet.
        /// </summary>
        /// <returns></returns>
        ImmutableList<IKubeCluster> GetAllMembers();

        /// <summary>
        /// Adds a member to this fleet.
        /// </summary>
        /// <param name="member"></param>
        void AddMember(IKubeCluster member);
    }
}
