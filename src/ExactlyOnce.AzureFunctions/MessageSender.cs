using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    class MessageSender
    {
        Func<Type, CloudQueue> mapMessageToQueue;

        public MessageSender(Func<Type, CloudQueue> mapMessageToQueue)
        {
            this.mapMessageToQueue = mapMessageToQueue;
        }

        public Task Publish(Guid messageId, object message, Dictionary<string, string> headers = null)
        {
            headers ??= new Dictionary<string, string>();

            var messageBytes = MessageSerializer.ToBytes(messageId, headers, message);

            var queueMessage = new CloudQueueMessage(messageBytes);
                    
            return mapMessageToQueue(message.GetType()).AddMessageAsync(queueMessage);
        }
    }
}