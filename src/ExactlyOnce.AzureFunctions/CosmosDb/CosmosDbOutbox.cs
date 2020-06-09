using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbOutbox
    {
        StorageConfiguration configuration;
        
        CosmosClient cosmosClient;
        Database database;
        Container container;

        public CosmosDbOutbox(StorageConfiguration configuration)
        {
            this.configuration = configuration;
        }

        public async Task Initialize()
        {
            cosmosClient = new CosmosClient(configuration.EndpointUri, configuration.PrimaryKey);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(configuration.DatabaseId);

            container = await database.CreateContainerIfNotExistsAsync(new ContainerProperties{
                Id = configuration.ContainerId, 
                PartitionKeyPath = "/Id",
                DefaultTimeToLive = -1 //No expiration unless explicitly set on item level
            });
        }

        public async Task<CosmosDbOutboxState> Get(Guid id)
        {
            using var response = await container.ReadItemStreamAsync(id.ToString(), PartitionKey.None);

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

            var item = JsonConvert.DeserializeObject<CosmosDbOutboxState>(content);

            var outputMessages = item.OutputMessagesText
                .Select(mt =>
                {
                    var (headers, message) = MessageSerializer.FromJson(mt);
                    var messageId = Guid.Parse(headers[Headers.MessageId]);

                    return (messageId, message);
                }).ToArray();

            item.OutputMessages = outputMessages.Select(i => i.message).ToArray();
            item.OutputMessagesIds = outputMessages.Select(i => i.messageId).ToArray();

            return item;
        }

        public Task CleanMessages(Guid messageId)
        {
            //No-op. TODO: handle with TimeToLive on the document

            return Task.CompletedTask;
        }

        public async Task Commit(Guid transactionId)
        {
            var state = await Get(transactionId);
            
            state.Id = state.MessageId;
            state.TimeToLiveSeconds = (int)TimeSpan.FromSeconds(100).TotalSeconds;

            var batch = container.CreateTransactionalBatch(PartitionKey.None)
                .DeleteItem(transactionId.ToString())
                .UpsertItem(state);

            var result = await batch.ExecuteAsync();

            if (result.IsSuccessStatusCode == false)
            {
                throw new Exception(result.ErrorMessage);
            }
        }

        public async Task Store(CosmosDbOutboxState outboxState)
        {
            outboxState.OutputMessagesText =
                Enumerable.Range(0, outboxState.OutputMessages.Length)
                    .Select(i =>
                    {
                        var messageId = outboxState.OutputMessagesIds[i];
                        var message = outboxState.OutputMessages[i];

                        return MessageSerializer.ToJson(messageId, new Dictionary<string, string>(), message);
                    }).ToArray();

            var response = await container.UpsertItemAsync(outboxState);

            // HINT: Outbox item should be created or re-updated (if there was a failure
            //       during previous commit).
            if (response.StatusCode != HttpStatusCode.Created &&
                response.StatusCode != HttpStatusCode.OK)
            {
                throw new Exception("Error storing outbox item");
            }
        }
    }
}