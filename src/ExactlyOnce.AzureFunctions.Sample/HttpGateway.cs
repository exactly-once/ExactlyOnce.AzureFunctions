using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class HttpGateway
    {
        IOnceExecutor execute;

        public HttpGateway(IOnceExecutor execute)
        {
            this.execute = execute;
        }

        [FunctionName(nameof(RequestFireAt))]
        public async Task<IActionResult> RequestFireAt(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
            [Queue("fire-attempt")] ICollector<FireAt> collector,
            ILogger log)
        {
            var requestId = req.Query["requestId"];

            log.LogInformation($"Processing RequestFireAt: requestId={requestId}");

            //HINT: any duplicated executions will return FireAt identical to the first execution
            var fireAt = await execute.Once(requestId,
                () => new FireAt
                {
                    AttemptId = requestId.ToGuid(),
                    GameId = req.Query["gameId"].ToGuid(),
                    Position = int.Parse(req.Query["position"])
                }
            );

            collector.Add(fireAt);

            return new OkObjectResult("New round requested.");
        }


        public class DummyState : State
        {
        }
    }
}