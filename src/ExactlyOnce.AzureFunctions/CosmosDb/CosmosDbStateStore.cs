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

        string databaseId = "ExactlyOnce";

        public async Task Initialize()
        {
            cosmosClient = new CosmosClient(EndpointUri, PrimaryKey);

            database = await cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId);
        }

        public async Task<CosmosDbE1Item> Load(Guid itemId, Type stateType)
        {
            Container container = await database
                .DefineContainer(stateType.Name, "/id")
                .CreateIfNotExistsAsync();
                
            using var response = await container.ReadItemStreamAsync(itemId.ToString(), new PartitionKey(itemId.ToString()));

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
            Container container = await database
                .DefineContainer(item.Item.GetType().Name, "/id")
                .CreateIfNotExistsAsync();

            ItemResponse<CosmosDbE1Content> response;

            if (item.ETag == null)
            {
                response = await container.CreateItemAsync(item.Item, new PartitionKey(item.Item.Id));
            }
            else
            {
                response = await container.UpsertItemAsync(
                    item.Item,
                    requestOptions: new ItemRequestOptions
                    {
                        IfMatchEtag = item.ETag,
                    });
            }

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new Exception("Optimistic concurrency exception on item persist");
            }

            item.ETag = response.Headers.ETag;
        }
    }
}