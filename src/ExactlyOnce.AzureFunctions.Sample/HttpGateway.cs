using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class HttpGateway
    {
        [FunctionName(nameof(RequestFireAt))]
        public async Task<IActionResult> RequestFireAt(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            FireAtRequest request,
            [ExactlyOnce("{RequestId}")] IOnceExecutor execute,
            [Queue("fire-attempt")] ICollector<FireAt> collector,
            ILogger log)
        {
            log.LogInformation($"Processing RequestFireAt: requestId={request.RequestId}");

            //HINT: any duplicated executions will return FireAt identical to the first execution
            var fireAt = await execute.Once(
                () => new FireAt
                {
                    AttemptId = request.RequestId.ToGuid(),
                    GameId = request.GameId.ToGuid(),
                    Position = request.Position
                }
            );

            collector.Add(fireAt);

            return new OkObjectResult("New round requested.");
        }

        public class FireAtRequest
        {
            public string RequestId { get; set; }
            public string GameId {get; set; }
            public int Position {get; set; }
        }
    }
}