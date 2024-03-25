// ReSharper disable ClassNeverInstantiated.Global
// ReSharper disable UnusedAutoPropertyAccessor.Global
namespace Snd.Sdk.Kubernetes
{
    /// <summary>
    /// Wrapper for a custom resource definition properties used in API calls.
    /// </summary>
    public sealed class NamespacedCrd
    {
        /// <summary>
        /// Group for this CRD, for example myapps.io.
        /// </summary>
        public string Group { get; set; }

        /// <summary>
        /// Plural name for this CRD, for example myresources.
        /// </summary>
        public string Plural { get; set; }

        /// <summary>
        /// Version used, for example v1alpha1.
        /// </summary>
        public string Version { get; set; }
    }
}