using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbStateStore
    {
        static readonly string EndpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
        
        static readonly string PrimaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

        CosmosClient cosmosClient;
        Database database;
        Container container;

        string databaseId = "ExactlyOnce";
        string containerId = "State";
        string partitionKeyPath = "/Id";

        public async Task Initialize()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);

            container = await database.CreateContainerIfNotExistsAsync(containerId, partitionKeyPath);
        }

        public async Task<CosmosDbE1Item> Load(Guid itemId, Type stateType)
        {
            using var response = await container.ReadItemStreamAsync(itemId.ToString(), PartitionKey.None);

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

            var item = JsonConvert.DeserializeObject(content, stateType);

            var result = new CosmosDbE1Item
            {
                ETag = response.Headers.ETag,
                Item = (CosmosDbE1Content)item
            };

            return result;
        }

        public async Task Persist(CosmosDbE1Item item)
        {
            var response = await container.UpsertItemAsync(
                item.Item, 
                requestOptions: new ItemRequestOptions
                {
                    IfMatchEtag = item.ETag,
                });

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new Exception("Optimistic concurrency exception on item persist");
            }

            item.ETag = response.Headers.ETag;
        }
    }
}