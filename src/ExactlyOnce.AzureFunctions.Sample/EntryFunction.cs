using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

[assembly: InternalsVisibleTo("ExactlyOnce.AzureFunctions.Tests")]
[assembly: InternalsVisibleTo("ExactlyOnce.AzureFunctions.Console")]

namespace ExactlyOnce.AzureFunctions.Sample
{
    class EntryFunction
    {
        MessageProcessor messageProcessor;

        public EntryFunction(MessageProcessor messageProcessor)
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
