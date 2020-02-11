using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample.Domain;
using Microsoft.Azure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions.Sample
{
     class MessageSender
    {
        CloudQueue queue;

        public MessageSender(CloudQueue queue)
        {
            this.queue = queue;
        }

        public Task Publish(Message[] messages, Guid? runId = null)
        {
            var headers = runId.HasValue
                ? new Dictionary<string, string> {{"Message.RunId", runId.ToString()}}
                : new Dictionary<string, string>();
            
            var sendTasks = messages
                .Select(m =>
                {
                    var content = Serializer.Serialize(m, headers);

                    return new CloudQueueMessage(content);
                })
                .Select(qm => queue.AddMessageAsync(qm));
            

            return Task.WhenAll(sendTasks.ToArray());
        }
    }
}