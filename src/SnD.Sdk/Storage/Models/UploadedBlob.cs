using System;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>  
    /// Represents an uploaded blob.   
    /// Includes information such as the blob name, last modified timestamp, and content hash.  
    /// </summary>  
    public sealed class UploadedBlob
    {
        /// <summary>  
        /// Gets or sets the name of the uploaded blob.  
        /// </summary>  
        public string Name { get; set; }

        /// <summary>  
        /// Gets or sets the time when the blob was last modified.  
        /// </summary>  
        public DateTimeOffset LastModified { get; set; }

        /// <summary>  
        /// Gets or sets the hash of the content of the blob. This can be used for verification and data integrity checks.  
        /// </summary>  
        public string ContentHash { get; set; }
    }
}
