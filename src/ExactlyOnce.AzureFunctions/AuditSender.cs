using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Azure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    class AuditSender
    {
        CloudQueue auditQueue;

        public AuditSender(CloudQueue auditQueue)
        {
            this.auditQueue = auditQueue;
        }

        public Task Publish(string conversationId, int messageDelta)
        {
            var content = new Dictionary<string, string>
            {
                {Headers.ConversationId, conversationId },
                {Headers.AuditMessageDelta, messageDelta.ToString() }
            };

            var json = JsonSerializer.Serialize(content);

            return auditQueue.AddMessageAsync(new CloudQueueMessage(json));
        }
    }
}