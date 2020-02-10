using System;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample.Domain;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;
using Microsoft.WindowsAzure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions.Sample
{
    ///TODO: use https://docs.microsoft.com/en-us/azure/azure-functions/functions-dotnet-dependency-injection
    ///      for configuration etc.
    
    public static class EntryFunction
    {
        [FunctionName("EntryFunction")]
        public static async Task Run(
            [QueueTrigger("%ExactlyOnceInputQueue%", Connection = "")]CloudQueueMessage c, ILogger log)
        {
            log.LogInformation($"C# Queue trigger function processed: {c}");

            var (headers, message) = Serializer.Deserialize(c.AsBytes);

            //TODO: add stream store and create StateStore instance
            var invoker = new HandlerInvoker(null);
            var outputMessages = await invoker.Process(message as Message);

            var runId = Guid.Parse(headers["Message.RunId"]);

            //messageProcessed(runId, message, outputMessages);

            //TODO: enable sending out messages
            //await d.Send(outputMessages, runId);
        }
    }
}
