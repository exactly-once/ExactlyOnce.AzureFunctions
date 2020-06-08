using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    class AuditSender
    {
        QueueProvider queueProvider;

        public AuditSender(QueueProvider queueProvider)
        {
            this.queueProvider = queueProvider;
        }

        public Task Publish(string conversationId, Guid processedMessageId, Guid[] inflightMessageIds)
        {
            var content = new Dictionary<string, string>
            {
                {Headers.ConversationId, conversationId },
                {Headers.AuditProcessedMessageId, processedMessageId.ToString() },
                {Headers.AuditInFlightMessageIds, string.Join(",", inflightMessageIds) }
            };

            var json = JsonSerializer.Serialize(content);

            return queueProvider.GetQueue("audit").AddMessageAsync(new CloudQueueMessage(json));
        }
    }
}