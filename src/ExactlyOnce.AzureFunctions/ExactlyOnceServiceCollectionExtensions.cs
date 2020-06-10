using System;
using ExactlyOnce.AzureFunctions.CosmosDb;
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
            var messageRoutes = new RoutingConfiguration();
            var outboxConfiguration = new StorageConfiguration();

            services.AddSingleton<QueueProvider>();
            services.AddSingleton(p => handlerMap);
            services.AddSingleton(p => messageRoutes);
            services.AddSingleton(p => outboxConfiguration);

            services.AddLogging();
       
            services.AddScoped<HandlerInvoker>();
            services.AddSingleton<MessageSender>();
            services.AddSingleton<AuditSender>();
            services.AddSingleton<InMemoryLockManager>();
            services.AddScoped<IMessageProcessor>(sp =>
            {
                var exactlyOnce = sp.GetRequiredService<IExactlyOnce>();
                var handlerInvoker = sp.GetRequiredService<HandlerInvoker>();
                var messageSender = sp.GetRequiredService<MessageSender>();
                var auditSender = sp.GetRequiredService<AuditSender>();
                var lockManager = sp.GetRequiredService<InMemoryLockManager>();

                return new MessageProcessor(exactlyOnce, handlerInvoker, messageSender, auditSender, lockManager);
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
        StorageConfiguration storageConfiguration;

        internal ExactlyOnceConfiguration(HandlersMap handlersMap, RoutingConfiguration routingConfiguration, StorageConfiguration storageConfiguration)
        {
            this.handlersMap = handlersMap;
            this.routingConfiguration = routingConfiguration;
            this.storageConfiguration = storageConfiguration;
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

        public ExactlyOnceConfiguration ConfigureOutbox(Action<StorageConfiguration> configure)
        {
            configure(storageConfiguration);

            return this;
        }
    }
}