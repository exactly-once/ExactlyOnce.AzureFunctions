﻿using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Net;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Microsoft.IO;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions
{
    public class OutboxStore
    {
        OutboxConfiguration configuration;

        CosmosClient cosmosClient;
        Database database;
        Container container;

        private readonly JsonSerializer serializer = JsonSerializer.CreateDefault(new JsonSerializerSettings
        {
            TypeNameHandling = TypeNameHandling.All
        });

        private static readonly RecyclableMemoryStreamManager memoryStreamManager = new RecyclableMemoryStreamManager();

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

        public async Task<OutboxItem> Get(string id, CancellationToken cancellationToken = default)
        {
            using var response = await container.ReadItemStreamAsync(id, PartitionKey.None, cancellationToken: cancellationToken);

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return null;
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ErrorMessage);
            }

            using var streamReader = new StreamReader(response.Content);
            using var jsonTextReader = new JsonTextReader(streamReader);

            return serializer.Deserialize<OutboxItem>(jsonTextReader);
        }


        public async Task Commit(string transactionId, CancellationToken cancellationToken = default)
        {
            var outboxItem = await Get(transactionId, cancellationToken);

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

            var result = await batch.ExecuteAsync(cancellationToken);

            if (result.IsSuccessStatusCode == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        public async Task Store(OutboxItem outboxItem, CancellationToken cancellationToken = default)
        {
            await using var stream = memoryStreamManager.GetStream();
            await using var streamWriter = new StreamWriter(stream);
            using var writer = new JsonTextWriter(streamWriter);

            serializer.Serialize(writer, outboxItem);
            await streamWriter.FlushAsync().ConfigureAwait(false);
            stream.Position = 0;

            var response = await container.UpsertItemStreamAsync(stream, PartitionKey.None, cancellationToken: cancellationToken);

            // HINT: Outbox item should be created or re-updated (if there was a failure
            //       during previous commit).
            if (response.StatusCode != HttpStatusCode.Created &&
                response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Error storing outbox item");
            }
        }

        public Task Delete(string itemId, CancellationToken cancellationToken = default)
        {
            return container.DeleteItemAsync<OutboxItem>(itemId, PartitionKey.None, cancellationToken: cancellationToken);
        }
    }
}