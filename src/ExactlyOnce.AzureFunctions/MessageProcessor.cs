using System;
using System.Collections.Generic;
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
            var (headers, message) = MessageSerializer.FromBytes(queueItem.AsBytes);
            
            var messageId = Guid.Parse(headers[Headers.MessageId]);
            var conversationId = MakeSureConversationIsTracked(messageId, headers);

            var handler = handlerInvoker.GetHandler(message);
            var businessId = handler.GetBusinessId(message);

            var outputMessageIds = new List<Guid>();

            async Task Publish(Guid outputMessageId, object outputMessage)
            {
                var outputHeaders = new Dictionary<string, string>
                {
                    {Headers.ConversationId, conversationId}
                };

                await sender.Publish(outputMessageId, outputMessage, outputHeaders);

                outputMessageIds.Add(outputMessageId);
            }

            object[] Handle(object inputMessage, object state)
            {
                return handlerInvoker.Process(messageId, inputMessage, handler, state);
            }

            await exactlyOnce.Process(messageId, businessId, handler.DataType, message, Handle, Publish);

            await auditSender.Publish(conversationId, messageId, outputMessageIds.ToArray());

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