using System;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>  
    /// Represents a file on the SMB or NFS file share.   
    /// Includes information such as the file name, size, share item ID, permission set, and timestamps for creation and last modification.  
    /// </summary>  
    public sealed class ShareFile
    {
        /// <summary>  
        /// Gets or sets the name of the file.  
        /// </summary>  
        public string Name { get; set; }

        /// <summary>  
        /// Gets or sets the size of the file.  
        /// </summary>  
        public long Size { get; set; }

        /// <summary>  
        /// Gets or sets the identifier of the file.  
        /// </summary>  
        public string ShareItemId { get; set; }

        /// <summary>  
        /// Gets or sets the permission set of the file.  
        /// </summary>  
        public string PermissionSet { get; set; }

        /// <summary>  
        /// Gets or sets the time when the file was created.  
        /// </summary>  
        public DateTimeOffset? CreatedOn { get; set; }

        /// <summary>  
        /// Gets or sets the time when the file was last modified.  
        /// </summary>  
        public DateTimeOffset? LastModifiedOn { get; set; }
    }
}
