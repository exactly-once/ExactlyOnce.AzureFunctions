using Microsoft.Azure.WebJobs;

namespace ExactlyOnce.AzureFunctions
{
    internal class ResponseCollectorConverter<T> : IConverter<ExactlyOnceResponseAttribute, IAsyncCollector<T>>
    {
        private readonly ExactlyOnceExtensions configProvider;

        public ResponseCollectorConverter(ExactlyOnceExtensions configProvider)
        {
            this.configProvider = configProvider;
        }

        public IAsyncCollector<T> Convert(ExactlyOnceResponseAttribute attribute)
        {
            var context = this.configProvider.CreateContext(attribute);

            return new ResponseCollector<T>(context);
        }
    }
}