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
        static readonly string EndpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
        
        static readonly string PrimaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

        CosmosClient cosmosClient;
        Database database;
        Container container;

        string databaseId = "ExactlyOnce";
        string containerId = "Outbox";
        string partitionKeyPath = "/Id";

        public async Task Initialize()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

            container = await database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
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

            item.OutputMessages = item.OutputMessagesText
                .Select(mt =>
                {
                    var (_, b) = Serializer.TextDeserialize(mt);

                    return (Message) b;
                }).ToArray();

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

            await Store(state);
        }

        public async Task Store(CosmosDbOutboxState outboxState)
        {
            outboxState.OutputMessagesText = outboxState.OutputMessages
                .Select(m => Serializer.TextSerialize(m, new Dictionary<string, string>())).ToArray();

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