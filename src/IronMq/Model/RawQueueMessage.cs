namespace DR.Common.IronMq.Model
{
    /// <summary>
    ///     Raw, unstranslated message from IQueueService
    /// </summary>
    public class RawQueueMessage
    {
        /// <summary>
        ///     Message id. Not to be confused with the resource identifier.
        /// </summary>
        public string Id { get; set; }

        /// <summary>
        ///     Message reservation id.
        ///     Used for deleting message when it is processed
        /// </summary>
        public string ReservationId { get; set; }

        /// <summary>
        ///     The number of times a message has been reserved.
        /// </summary>
        public int ReservedCount { get; set; }

        /// <summary>
        ///     Content of the message, must be translated/deserialized before it is handled.
        /// </summary>
        public string Body { get; set; }
    }
}