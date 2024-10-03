using System;
using Snd.Sdk.Storage.Models.Base;

namespace Snd.Sdk.Storage.Models
{
    /// <summary>  
    /// Represents the response returned when a message is revealed in the queue, after being hidden.   
    /// Includes information such as the message ID, when the message will be visible in the queue again, and the delete handle of the message.  
    /// </summary> 
    public sealed class QueueReleaseResponse : IMessageReleaseResponse
    {
        /// <summary>  
        /// Gets or sets the identifier of the message in the queue.  
        /// </summary>  
        public string MessageId { get; set; }

        /// <summary>  
        /// Gets or sets the time at which the message will be visible in the queue again.  
        /// </summary>  
        public DateTimeOffset VisibleAt { get; set; }

        /// <summary>  
        /// Gets or sets the delete handle of the message. This can be used to delete a message from the queue.  
        /// </summary>  
        public string DeleteHandle { get; set; }
    }
}
