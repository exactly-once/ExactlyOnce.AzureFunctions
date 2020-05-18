using ExactlyOnce.AzureFunctions.CosmosDb;
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
            services.AddSingleton(p => CreateAuditSender());
            services.AddScoped<MessageProcessor>();

            //State based exactly-once 
            /* 
            services.AddSingleton<StateBasedExactlyOnce>();
            services.AddSingleton<IExactlyOnce>(sp => sp.GetRequiredService<StateBasedExactlyOnce>());
            */

            services.AddSingleton<CosmosDbOutbox>();
            services.AddSingleton<CosmosDbStateStore>();
            services.AddSingleton<CosmosDbExactlyOnce>();
            services.AddSingleton<IExactlyOnce>(sp =>
            {
                var instance = sp.GetRequiredService<CosmosDbExactlyOnce>();
                instance.Initialize().GetAwaiter().GetResult();

                return instance;
            });

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

        internal static AuditSender CreateAuditSender() => new AuditSender(GetQueue("audit"));
        
        internal static MessageSender CreateMessageSender(string queueName = "test") => new MessageSender(GetQueue(queueName));

        internal static CloudQueue GetQueue(string queueName)
        {
            var storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse("UseDevelopmentStorage=true;");
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);

            queue.CreateIfNotExists();

            return queue;
        }
    }

    public class ExactlyOnceConfiguration
    {
        HandlersMap handlersMap;

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