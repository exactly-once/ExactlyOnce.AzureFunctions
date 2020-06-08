using System;
using ExactlyOnce.AzureFunctions.CosmosDb;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace ExactlyOnce.AzureFunctions
{
    public class QueueProvider
    {
        RoutingConfiguration configuration;

        public QueueProvider(RoutingConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public CloudQueue GetQueue(string queueName)
        {
            var storageAccount = Microsoft.Azure.Storage.CloudStorageAccount.Parse(configuration.ConnectionString);
            
            CloudQueueClient queueClient = storageAccount.CreateCloudQueueClient();
            CloudQueue queue = queueClient.GetQueueReference(queueName);

            queue.CreateIfNotExists();

            return queue;
        }
    }

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
            var messageRoutes = new RoutingConfiguration();
            var outboxConfiguration = new OutboxConfiguration();

            services.AddSingleton<QueueProvider>();
            services.AddSingleton(p => handlerMap);
            services.AddSingleton(p => messageRoutes);
            services.AddSingleton(p => outboxConfiguration);

            services.AddLogging();
       
            services.AddScoped<HandlerInvoker>();
            services.AddSingleton<MessageSender>();
            services.AddSingleton<AuditSender>();
            services.AddScoped<IMessageProcessor>(sp =>
            {
                var exactlyOnce = sp.GetRequiredService<IExactlyOnce>();
                var handlerInvoker = sp.GetRequiredService<HandlerInvoker>();
                var messageSender = sp.GetRequiredService<MessageSender>();
                var auditSender = sp.GetRequiredService<AuditSender>();

                return new MessageProcessor(exactlyOnce, handlerInvoker, messageSender, auditSender);
            });

            services.AddSingleton<CosmosDbOutbox>();
            services.AddSingleton<CosmosDbStateStore>();
            services.AddSingleton<CosmosDbExactlyOnce>();
            services.AddSingleton<IExactlyOnce>(sp =>
            {
                var instance = sp.GetRequiredService<CosmosDbExactlyOnce>();
                instance.Initialize().GetAwaiter().GetResult();

                return instance;
            });

            return new ExactlyOnceConfiguration(handlerMap, messageRoutes, outboxConfiguration);
        }
    }

    public class ExactlyOnceConfiguration
    {
        HandlersMap handlersMap;
        RoutingConfiguration routingConfiguration;
        OutboxConfiguration outboxConfiguration;

        internal ExactlyOnceConfiguration(HandlersMap handlersMap, RoutingConfiguration routingConfiguration, OutboxConfiguration outboxConfiguration)
        {
            this.handlersMap = handlersMap;
            this.routingConfiguration = routingConfiguration;
            this.outboxConfiguration = outboxConfiguration;
        }

        public ExactlyOnceConfiguration AddHandler<THandler>()
        {
            handlersMap.AddHandler<THandler>();

            return this;
        }

        public ExactlyOnceConfiguration ConfigureRouting(Action<RoutingConfiguration> configure)
        {
            configure(routingConfiguration);

            return this;
        }

        public ExactlyOnceConfiguration ConfigureOutbox(Action<OutboxConfiguration> configure)
        {
            configure(outboxConfiguration);

            return this;
        }
    }
}