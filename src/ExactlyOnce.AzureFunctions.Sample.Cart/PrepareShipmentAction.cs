using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public static class PrepareShipmentAction
    {
        [FunctionName("PrepareShipmentAction")]
        public static void Run(
            [QueueTrigger("prepare-shipment")]Envelope command, 
            string id,
            ILogger log)
        {
            log.LogInformation($"Prepare shipment processed: {command.Content}");
        }
    }
}
