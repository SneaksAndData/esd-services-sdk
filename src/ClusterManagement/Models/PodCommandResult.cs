using System.Diagnostics.CodeAnalysis;

namespace Snd.Sdk.ClusterManagement.Models
{
    /// <summary>
    /// Result of an `exec` pod command.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class PodCommandResult
    {
        /// <summary>
        /// Command stdout.
        /// </summary>
        public string StdOut { get; set; }

        /// <summary>
        /// Command stderr.
        /// </summary>
        public string StdErr { get; set; }

        /// <summary>
        /// Command exit code.
        /// </summary>
        public int ExitCode { get; set; }
    }
}
