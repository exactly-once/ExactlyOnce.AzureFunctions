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
        IExactlyOnce exactlyOnce;

        public MessageProcessor(IExactlyOnce exactlyOnce, HandlerInvoker handlerInvoker, MessageSender sender, AuditSender auditSender)
        {
            this.exactlyOnce = exactlyOnce;
            this.handlerInvoker = handlerInvoker;
            this.sender = sender;
            this.auditSender = auditSender;
        }

        public async Task Process(CloudQueueMessage queueItem)
        {
            var (headers, m) = Serializer.Deserialize(queueItem.AsBytes);

            var message = (Message) m;

            var conversationId = MakeSureConversationIsTracked(message.Id, headers);

            var handler = handlerInvoker.GetHandler(message);
            var businessId = handler.GetBusinessId(message);

            async Task Publish(Message[] messages)
            {
                var outputHeaders = new Dictionary<string, string>
                {
                    {Headers.ConversationId, conversationId}
                };
                await sender.Publish(messages, outputHeaders);
                await auditSender.Publish(conversationId, message.Id, messages.Select(m => m.Id).ToArray());
            }

            Message[] Handle(Message inputMessage, object state)
            {
                return handlerInvoker.Process(inputMessage, handler, state);
            }

            await exactlyOnce.Process(businessId, handler.DataType, message, Handle, Publish);
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