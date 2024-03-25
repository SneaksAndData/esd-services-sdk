namespace Snd.Sdk.Storage.Models
{
    /// <summary>
    /// Result of a merge operation for schema-less entity.
    /// </summary>
    public sealed class MergeEntityResult
    {
        /// <summary>
        /// If the operation was successful.
        /// </summary>
        public bool IsSuccessful { get; set; }

        /// <summary>
        /// Operation trace, if any.
        /// </summary>
        public string Trace { get; set; }
    }
}
