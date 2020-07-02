using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using System.Web.Http;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class HttpGateway
    {
        IOnceExecutor executor;

        public HttpGateway(IOnceExecutor executor)
        {
            this.executor = executor;
        }

        [FunctionName(nameof(RequestFireAt))]
        public async Task<IActionResult> RequestFireAt(
            [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = null)]
            HttpRequest req,
            [Queue("fire-attempt")] ICollector<FireAt> collector,
            ILogger log)
        {
            log.LogInformation("Processing RequestFireAt: requestId =.");

            var requestId = req.Query["requestId"];

            var output = await executor.Once<FireAt>(requestId)
                .On<DummyState>(Guid.Empty,
                    _ => new SendMessage<FireAt>(new FireAt
                    {
                        AttemptId = ToGuid(requestId),
                        GameId = ToGuid(req.Query["gameId"]),
                        Position = int.Parse(req.Query["position"])
                    })
                );

            if (output is SendMessage<FireAt> send)
            {
                collector.Add(send.Message);

                return new OkObjectResult("New round requested.");
            }

            return new BadRequestErrorMessageResult("Ooops");
        }

        public static Guid ToGuid(string src)
        {
            var bytes = new SHA1CryptoServiceProvider().ComputeHash(Encoding.UTF8.GetBytes(src));
            Array.Resize(ref bytes, 16);
            return new Guid(bytes);
        }

        public class DummyState : State
        {
        }
    }
}