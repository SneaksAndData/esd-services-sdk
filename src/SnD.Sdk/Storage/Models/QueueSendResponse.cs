using System;
using System.Collections.Generic;
using System.Text;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>  
    /// Represents the response returned when a message is sent to a queue.   
    /// Includes information such as the message ID, the delete handle of the message, and the time the message was inserted.  
    /// </summary>  
    public sealed class QueueSendResponse
    {
        /// <summary>  
        /// Gets or sets the identifier of the message in the queue.  
        /// </summary>  
        public string MessageId { get; set; }

        /// <summary>  
        /// Gets or sets the delete handle of the message. This can be used to delete a message from the queue.  
        /// </summary>  
        public string DeleteHandle { get; set; }

        /// <summary>  
        /// Gets or sets the time at which the message was inserted into the queue.  
        /// </summary>  
        public DateTimeOffset InsertedAt { get; set; }
    }
}
