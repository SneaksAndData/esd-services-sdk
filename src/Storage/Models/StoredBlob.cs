using System;
using System.Collections.Generic;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>
    /// Blob object metadata.
    /// </summary>
    public sealed class StoredBlob
    {
        /// <summary>
        /// Additional metadata attached to this object.
        /// </summary>
        public IDictionary<string, string> Metadata { get; set; }

        /// <summary>
        /// Content hashsum.
        /// </summary>
        public string ContentHash { get; set; }

        /// <summary>
        /// Content encoding, for example utf-8.
        /// </summary>
        public string ContentEncoding { get; set; }

        /// <summary>
        /// Content type, for example text/plain.
        /// </summary>
        public string ContentType { get; set; }

        /// <summary>
        /// Content length in bytes.
        /// </summary>
        public long? ContentLength { get; set; }

        /// <summary>
        /// Blob filename. May contain full path, depending on the actual storage.
        /// </summary>
        public string Name { get; set; }

        /// <summary>
        /// Last modified timestamp.
        /// </summary>
        public DateTimeOffset? LastModified { get; set; }

        /// <summary>
        /// Created on timestamp.
        /// </summary>
        public DateTimeOffset? CreatedOn { get; set; }
    }
}
