using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.DependencyInjection;

namespace ExactlyOnce.AzureFunctions
{
    public static class ExactlyOnceServiceCollectionExtensions
    {
        public static ExactlyOnceConfiguration AddExactlyOnce(this IServiceCollection services)
        {
            services.AddLogging();
       
            services.AddSingleton(p => CreateStateStore());
            services.AddScoped<HandlerInvoker>();
            services.AddSingleton(p => CreateMessageSender());
            services.AddScoped<MessageProcessor>();
       
            var handlerMap = new HandlersMap();

            services.AddSingleton(p => handlerMap);

            return new ExactlyOnceConfiguration(handlerMap);
        }

        internal static StateStore CreateStateStore()
        {
            var table = CloudStorageAccount
                .DevelopmentStorageAccount
                .CreateCloudTableClient()
                .GetTableReference("EndpointStore");

            table.CreateIfNotExists();

            var store = new StateStore(table);
            
            return store;
        }

        internal static MessageSender CreateMessageSender()
        {
            var storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference("test");

            queue.CreateIfNotExists();

            var sender = new MessageSender(queue);

            return sender;
        }
    }

    public class ExactlyOnceConfiguration
    {
        private readonly HandlersMap handlersMap;

        internal ExactlyOnceConfiguration(HandlersMap handlersMap)
        {
            this.handlersMap = handlersMap;
        }

        public ExactlyOnceConfiguration AddHandler<THandler>()
        {
            handlersMap.AddHandler<THandler>();

            return this;
        }
    }
}