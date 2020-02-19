using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    class MessageProcessor
    {
        HandlerInvoker handlerInvoker;
        MessageSender sender;
        AuditSender auditSender;

        public MessageProcessor(HandlerInvoker handlerInvoker, MessageSender sender, AuditSender auditSender)
        {
            this.handlerInvoker = handlerInvoker;
            this.sender = sender;
            this.auditSender = auditSender;
        }

        public async Task Process(CloudQueueMessage queueItem)
        {
            var (headers, m) = Serializer.Deserialize(queueItem.AsBytes);

            var message = (Message) m;

            var conversationId = MakeSureConversationIsTracked(message.Id, headers);

            var outputMessages = await handlerInvoker.Process(message);

            var outputHeaders = new Dictionary<string, string>
            {
                { Headers.ConversationId, conversationId }
            };

            await sender.Publish(outputMessages, outputHeaders);

            await auditSender.Publish(conversationId, message.Id, outputMessages.Select(m => m.Id).ToArray());
        }

        string MakeSureConversationIsTracked(Guid messageId, Dictionary<string, string> headers)
        {
            if (headers.ContainsKey(Headers.ConversationId) == false)
            {
                headers[Headers.ConversationId] = messageId.ToString();
            }

            return headers[Headers.ConversationId];
        }
    }
}