using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public static class PrepareShipmentAction
    {
        [FunctionName("PrepareShipmentAction")]
        public static void Run(
            [QueueTrigger("prepare-shipment")]PrepareShipment command, 
            [ExactlyOnceResponse] IAsyncCollector<PrepareShipmentResponse> collector,
            ILogger log)
        {
            log.LogInformation($"Prepare shipment processed: {command.OrderId}");

            collector.AddAsync(new PrepareShipmentResponse{OrderId = command.OrderId});
        }
    }
}
