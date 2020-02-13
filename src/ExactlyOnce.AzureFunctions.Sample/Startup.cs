using ExactlyOnce.AzureFunctions.Sample;
using Microsoft.Azure.Cosmos.Table;
using Microsoft.Azure.Functions.Extensions.DependencyInjection;
using Microsoft.Azure.Storage.Queue;
using Microsoft.Extensions.DependencyInjection;

[assembly: FunctionsStartup(typeof(Startup))]

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class Startup : FunctionsStartup
    {
        public override void Configure(IFunctionsHostBuilder builder)
        {
            builder.Services.AddLogging();

            builder.Services.AddScoped(p => CreateStateStore());
            builder.Services.AddScoped<HandlerInvoker>();
            builder.Services.AddScoped(p => CreateMessageSender());
            builder.Services.AddScoped<MessageProcessor>();
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
}