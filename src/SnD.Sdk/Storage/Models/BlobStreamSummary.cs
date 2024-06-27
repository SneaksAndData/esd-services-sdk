using System.Diagnostics.CodeAnalysis;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>
    /// Summary of a blob stream read operation.
    /// </summary>
    [ExcludeFromCodeCoverage]
    public sealed class BlobStreamSummary
    {
        /// <summary>
        /// If operation was a success.
        /// </summary>
        public bool IsSuccess { get; }

        /// <summary>
        /// Number of bytes written to a read stream.
        /// </summary>
        public int BytesWritten { get; }

        /// <summary>
        /// Error, if any.
        /// </summary>
        public string ErrorMessage { get; }
    }
}
