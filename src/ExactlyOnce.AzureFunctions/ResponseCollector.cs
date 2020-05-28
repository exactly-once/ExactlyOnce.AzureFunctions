using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace ExactlyOnce.AzureFunctions
{
    public class ResponseCollector<T>: IAsyncCollector<T>
    {
        ResponseCollectorContext context;

        internal ResponseCollector(ResponseCollectorContext context)
        {
            this.context = context;
        }

        public Task AddAsync(T message, CancellationToken cancellationToken = new CancellationToken())
        {
            var (headers, _) = MessageSerializer.FromJson(context.ResolvedAttribute.InputMessage);
            var inputMessageId = headers[Headers.MessageId];
            
            var responseId = inputMessageId.ToGuid();

            return context.Sender.Publish(responseId, message);
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            return Task.CompletedTask;
        }
    }
}