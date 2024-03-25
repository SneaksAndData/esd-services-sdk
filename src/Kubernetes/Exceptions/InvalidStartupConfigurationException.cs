using System;

namespace Snd.Sdk.Kubernetes.Exceptions
{
    /// <summary>
    /// Markup type for invalid/missing kube config.
    /// </summary>
    public sealed class InvalidStartupConfigurationException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="InvalidStartupConfigurationException"/>.
        /// </summary>
        /// <param name="message"></param>
        public InvalidStartupConfigurationException(string message) : base(message)
        {

        }
    }
}
