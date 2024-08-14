using System;

namespace Snd.Sdk.Kubernetes.Exceptions
{
    /// <summary>
    /// Thrown when a stateful set hasn't completed a scaling operation.
    /// </summary>
    public class StatefulSetNotReadyException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="StatefulSetNotReadyException"/>.
        /// </summary>
        /// <param name="message"></param>
        public StatefulSetNotReadyException(string message) : base(message)
        {

        }
    }
}
