using System;

namespace Snd.Sdk.Kubernetes.Exceptions
{
    /// <summary>
    /// Thrown when a shared file system was not initialized.
    /// </summary>
    public class SharedFileSystemNotInitializedException : Exception
    {
        /// <summary>
        /// Creates an instance of <see cref="SharedFileSystemNotInitializedException"/>.
        /// </summary>
        /// <param name="message"></param>
        public SharedFileSystemNotInitializedException(string message) : base(message)
        {

        }
    }
}
