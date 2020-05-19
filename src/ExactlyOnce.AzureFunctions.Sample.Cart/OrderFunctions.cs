using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public static class OrderFunctions
    {
        [FunctionName("PlaceOrder")]
        public static async Task<IActionResult> PlaceOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Queue("%ExactlyOnceInputQueue%", Connection = "AzureWebJobsStorage")] ICollector<string> queueCollector,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            var orderId = data?.orderId;

            var envelope = Serializer.TextSerialize(
                    new PlaceOrder {Id = Guid.NewGuid(), OrderId = orderId}, new Dictionary<string, string>());

            queueCollector.Add(envelope);

            return new CreatedResult($"Order {orderId} created", orderId);
        }

        [FunctionName("ApproveOrder")]
        public static async Task<IActionResult> ApproveOrder(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)] HttpRequest req,
            [Queue("%ExactlyOnceInputQueue%", Connection = "AzureWebJobsStorage")] ICollector<string> queueCollector,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            
            var orderId = data?.orderId;

            var envelope = Serializer.TextSerialize(
                new ApproveOrder { Id = Guid.NewGuid(), OrderId = orderId}, new Dictionary<string, string>());

            queueCollector.Add(envelope);

            return new CreatedResult($"Order {orderId} created", orderId);
        }
    }
}
