using System.Collections.Generic;
using System.Threading.Tasks;
using DR.Common.IronMq.Kermit.Model;

namespace DR.Common.IronMq.Kermit
{
    public interface IQueue<TSystem>
    {
        Task<IEnumerable<RawQueueMessage>> Peek(int maxCount);

        Task<IEnumerable<RawQueueMessage>> PeekDeadletter(int maxCount);


        /// <summary>
        /// Retrieve next message from queue and mark it as reserved for as many seconds as specified in timeout parameter
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="longPoll"></param>
        /// <returns></returns>
        Task<RawQueueMessage> RetrieveMessage(int timeout, bool longPoll = false);

        /// <summary>
        /// Retrieve next message from Deadletter queue and mark it as reserved for as many seconds as specified in timeout parameter
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="longPoll"></param>
        /// <returns></returns>
        Task<RawQueueMessage> RetrieveDeadletterMessage(int timeout, bool longPoll = false);

        /// <summary>
        /// Retrieve maxCount messages from queue and mark it as reserved for as many seconds as specified in timeout parameter
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="maxCount"></param>
        /// <param name="longPoll"></param>
        /// <returns></returns>
        Task<IEnumerable<RawQueueMessage>> RetrieveMessages(int timeout, int maxCount, bool longPoll = false);

        /// <summary>
        /// Retrieve maxCount Deadletter messages from Deadletter queue and mark it as reserved for as many seconds as specified in timeout parameter
        /// 
        /// </summary>
        /// <param name="timeout"></param>
        /// <param name="maxCount"></param>
        /// <param name="longPoll"></param>
        /// <returns></returns>
        Task<IEnumerable<RawQueueMessage>> RetrieveDeadletterMessages(int timeout, int maxCount, bool longPoll = false);

        /// <summary>
        /// Send new message to queue
        /// </summary>
        /// <param name="data"></param>
        /// <returns></returns>
        Task PostMessage(object data);

        /// <summary>
        /// Delete message from queue
        /// 
        /// Use this after processing a message, received from RetrieveMessage
        /// </summary>
        /// <param name="rawQueueMessage"></param>
        Task DeleteMessage(RawQueueMessage rawQueueMessage);

        /// <summary>
        /// Release message from queue
        /// 
        /// </summary>
        /// <param name="rawQueueMessage"></param>
        Task ReleaseMessage(RawQueueMessage rawQueueMessage);

        /// <summary>
        /// Move message to deaqdletter queue
        /// 
        /// </summary>
        /// <param name="rawQueueMessage"></param>
        Task MoveToDeadletterQueue(RawQueueMessage rawQueueMessage);

        /// <summary>
        /// Move message to queue
        /// 
        /// </summary>
        /// <param name="rawQueueMessage"></param>
        Task MoveToQueue(RawQueueMessage rawQueueMessage);

        /// <summary>
        /// The Queue Owner
        /// </summary>
        TSystem QueueOwner { get; }

        int DeadletterCount { get; }

        int MessageCount { get; }

        Task<IQueueInfo> QueueInfo();

        Task<IQueueInfo> DeadletterQueueInfo();
    }
}
