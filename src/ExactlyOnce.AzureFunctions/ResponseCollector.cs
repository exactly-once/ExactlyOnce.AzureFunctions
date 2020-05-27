using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.WebJobs;

namespace ExactlyOnce.AzureFunctions
{
    public class ResponseCollector<T>: IAsyncCollector<T>
    {
        ResponseCollectorContext context;

        public ResponseCollector(ResponseCollectorContext context)
        {
            this.context = context;
        }

        public Task AddAsync(T message, CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }

        public Task FlushAsync(CancellationToken cancellationToken = new CancellationToken())
        {
            throw new System.NotImplementedException();
        }
    }
}