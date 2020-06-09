using Microsoft.Azure.Storage.Queue;

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
}