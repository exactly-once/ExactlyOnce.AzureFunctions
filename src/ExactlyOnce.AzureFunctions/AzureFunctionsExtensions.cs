using System;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Extensions.DependencyInjection;

namespace ExactlyOnce.AzureFunctions
{
    public static class ExactlyOnceHostingExtensions
    {
        public static IWebJobsBuilder AddExactlyOnce(this IWebJobsBuilder builder,
            Action<ExactlyOnceConfiguration> configure)
        {
            var configuration = builder.Services.RegisterServices();

            configure(configuration);

            return builder;
        }

        static ExactlyOnceConfiguration RegisterServices(this IServiceCollection services)
        {
            var outboxConfiguration = new OutboxConfiguration();
            var configuration = new ExactlyOnceConfiguration(outboxConfiguration);

            services.AddLogging();

            services.AddSingleton<IOnceExecutor>(sp =>
            {
                var stateStore = (IStateStore) sp.GetRequiredService(configuration.StateStoreType);
                var client = sp.GetRequiredService<CosmosClient>();

                var outboxStore = new OutboxStore(client, outboxConfiguration);
                var processor = new ExactlyOnceProcessor(outboxStore, stateStore);
                var onceExecutor = new OnceExecutor(processor);

                return onceExecutor;
            });

            return configuration;
        }
    }

    public class ExactlyOnceConfiguration
    {
        OutboxConfiguration outboxConfiguration;
        public Type StateStoreType;

        internal ExactlyOnceConfiguration(OutboxConfiguration outboxConfiguration)
        {
            this.outboxConfiguration = outboxConfiguration;
        }

        public ExactlyOnceConfiguration ConfigureOutbox(Action<OutboxConfiguration> configure)
        {
            configure(outboxConfiguration);

            return this;
        }

        public void StateStoreIs<T>() where T : IStateStore
        {
            StateStoreType = typeof(T);
        }
    }
}