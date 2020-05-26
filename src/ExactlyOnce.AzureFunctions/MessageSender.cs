using System;
using System.Collections.Generic;
using System.Linq;
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

        public Task Publish(Message[] messages, Dictionary<string, string> headers = null)
        {
            ThrowIfAnyMessageWithEmptyId(messages);

            headers ??= new Dictionary<string, string>();

            var sendTasks = messages
                .Select(m =>
                {
                    var content = Serializer.Serialize(m, headers);

                    var queueMessage = new CloudQueueMessage(content);
                    
                    return mapMessageToQueue(m.GetType()).AddMessageAsync(queueMessage);
                });

            return Task.WhenAll(sendTasks.ToArray());
        }

        private static void ThrowIfAnyMessageWithEmptyId(Message[] messages)
        {
            var messagesWithEmptyIds = messages.Where(m => m.Id == Guid.Empty).ToArray();

            if (messagesWithEmptyIds.Any())
            {
                var messageTypeNames = string.Join(
                    ",",
                    messagesWithEmptyIds.Select(m => $"{m.GetType().Name}")
                    );

                throw new Exception($"Can't send [{messageTypeNames}] messages with empty identifiers.");
            }
        }
    }
}