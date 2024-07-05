using System;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>
    /// Queue element model.
    /// </summary>
    public sealed class QueueElement
    {
        /// <summary>  
        /// Gets or sets the content of the queue element as binary data.  
        /// </summary>  
        public BinaryData Content { get; set; }

        /// <summary>  
        /// Gets or sets the identifier of the queue element.  
        /// </summary>  
        public string ElementId { get; set; }

        /// <summary>  
        /// Gets or sets the delete handle of the queue element. This can be used to delete a message from the queue.  
        /// </summary>  
        public string DeleteHandle { get; set; }

        /// <summary>  
        /// Gets or sets the dequeue count of the queue element. This indicates how many times a message has been retrieved from the queue.  
        /// </summary>  
        public long? DequeueCount { get; set; }
    }
}
