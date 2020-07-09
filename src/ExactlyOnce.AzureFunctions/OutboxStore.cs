using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    public class OutboxStore
    {
        OutboxConfiguration configuration;
        Container container;

        CosmosClient cosmosClient;
        Database database;

        JsonSerializerSettings settings = new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        };

        public OutboxStore(CosmosClient cosmosClient, OutboxConfiguration configuration)
        {
            this.cosmosClient = cosmosClient;
            this.configuration = configuration;

            Initialize().GetAwaiter().GetResult();
        }

        async Task Initialize()
        {
            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(configuration.DatabaseId);

            container = await database.CreateContainerIfNotExistsAsync(new ContainerProperties
            {
                Id = configuration.ContainerId,
                PartitionKeyPath = "/None",
                DefaultTimeToLive = -1 //No expiration unless explicitly set on item level
            });
        }

        public async Task<OutboxItem> Get(string id)
        {
            using var response = await container.ReadItemStreamAsync(id, PartitionKey.None);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ErrorMessage);
            }

            using var streamReader = new StreamReader(response.Content);

            var content = await streamReader.ReadToEndAsync();

            var item = JsonConvert.DeserializeObject<OutboxItem>(content, settings);

            return item;
        }


        public async Task Commit(string transactionId)
        {
            var outboxItem = await Get(transactionId);

            //HINT: outbox item has already been committed
            if (outboxItem == null)
            {
                return;
            }

            outboxItem.Id = outboxItem.RequestId;
            outboxItem.TimeToLiveSeconds = (int)configuration.RetentionPeriod.TotalSeconds;

            var batch = container.CreateTransactionalBatch(PartitionKey.None)
                .DeleteItem(transactionId)
                .UpsertItem(outboxItem);

            var result = await batch.ExecuteAsync();

            if (result.IsSuccessStatusCode == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        public async Task Store(OutboxItem outboxItem)
        {
            var json = JsonConvert.SerializeObject(outboxItem, settings);

            await using var stream = new MemoryStream();
            await using var writer = new StreamWriter(stream);
            writer.Write(json);
            writer.Flush();
            stream.Position = 0;

            var response = await container.UpsertItemStreamAsync(stream, PartitionKey.None);

            // HINT: Outbox item should be created or re-updated (if there was a failure
            //       during previous commit).
            if (response.StatusCode != HttpStatusCode.Created &&
                response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Error storing outbox item");
            }
        }

        public Task Delete(string itemId)
        {
            return container.DeleteItemAsync<OutboxItem>(itemId, PartitionKey.None);
        }
    }
}