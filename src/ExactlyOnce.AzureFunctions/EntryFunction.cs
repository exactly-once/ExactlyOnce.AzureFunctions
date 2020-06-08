using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    class EntryFunction
    {
        IMessageProcessor messageProcessor;

        public EntryFunction(IMessageProcessor messageProcessor)
        {
            this.messageProcessor = messageProcessor;
        }

        [FunctionName("EntryFunction")]
        public async Task Run(
            [QueueTrigger("%ExactlyOnceInputQueue%")]CloudQueueMessage queueItem, 
            ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {queueItem}");

            await messageProcessor.Process(queueItem);
        }
    }
}
