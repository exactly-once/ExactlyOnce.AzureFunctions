using System;
using ExactlyOnce.AzureFunctions.CosmosDb;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace ExactlyOnce.AzureFunctions
{
    public static class ExactlyOnceHostingExtensions
    {
        public static IWebJobsBuilder AddExactlyOnce(this IWebJobsBuilder builder,
            Action<ExactlyOnceConfiguration> configure)
        {
            builder.AddExtension<ExactlyOnceExtensions>();

            var configuration = builder.Services.RegisterServices();

            configure(configuration);

            return builder;
        }

        static ExactlyOnceConfiguration RegisterServices(this IServiceCollection services)
        {
            var handlerMap = new HandlersMap();
            var messageRoutes = new MessageRoutes();

            services.AddSingleton(p => handlerMap);
            services.AddSingleton(p => messageRoutes);

            services.AddLogging();
       
            services.AddSingleton(p => CreateStateStore());
            services.AddScoped<HandlerInvoker>();
            services.AddSingleton(p => CreateMessageSender(messageRoutes));
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

            return new ExactlyOnceConfiguration(handlerMap, messageRoutes);
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

        internal static MessageSender CreateMessageSender(MessageRoutes routes)
        {
            return new MessageSender(messageType => GetQueue(routes.Routes[messageType]));
        }

        internal static MessageSender CreateMessageSender(string destination)
        {
            return new MessageSender(messageType => GetQueue(destination));
        }

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
        MessageRoutes messageRoutes;

        internal ExactlyOnceConfiguration(HandlersMap handlersMap, MessageRoutes messageRoutes)
        {
            this.handlersMap = handlersMap;
            this.messageRoutes = messageRoutes;
        }

        public ExactlyOnceConfiguration AddHandler<THandler>()
        {
            handlersMap.AddHandler<THandler>();

            return this;
        }

        public ExactlyOnceConfiguration AddMessageRoute<T>(string destination)
        {
            messageRoutes.Routes.Add(typeof(T), destination);

            return this;
        }
    }
}