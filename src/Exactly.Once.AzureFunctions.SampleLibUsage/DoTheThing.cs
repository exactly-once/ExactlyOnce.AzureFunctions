using System.IO;
using System.Threading.Tasks;
using Exactly.Once.AzureFunctions.SampleLibUsage.Api;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Exactly.Once.AzureFunctions.SampleLibUsage
{
    public class DoTheThing
    {
        IOnceExecutor execute;

        public DoTheThing(IOnceExecutor execute)
        {
            this.execute = execute;
        }

        [FunctionName("DoTheThing")]
        public async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "get", "post", Route = null)] HttpRequest req,
            ILogger log)
        {
            log.LogInformation("C# HTTP trigger function processed a request.");

            string stateId = req.Query["stateId"];
            string requestId = req.Query["requestId"];

            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();
            dynamic data = JsonConvert.DeserializeObject(requestBody);
            requestId ??= data?.requestId;

            var sideEffects = await execute.Once(requestId).On<Counter>(stateId, counter =>
            {
                counter.Value++;

                return new SideEffect[]
                {
                    new SendMessageSideEffect()
                };
            });

            foreach (var sideEffect in sideEffects)
            {
                Apply(sideEffect, log);
            }

            string responseMessage = string.IsNullOrEmpty(requestId)
                ? "This HTTP triggered function executed successfully. Pass a name in the query string or in the request body for a personalized response."
                : $"Hello, {requestId}. This HTTP triggered function executed successfully.";

            return new OkObjectResult(responseMessage);
        }

        static void Apply(SideEffect sideEffect, ILogger log)
        {
            log.LogInformation($"Applying: ${sideEffect.GetType().Name}");
        }
    }

    public class Counter : State
    {
        [JsonProperty("value")]
        public int Value { get; set; }
    }
}
