using System;
using System.IO;
using System.Net;
using System.Threading.Tasks;
using Exactly.Once.AzureFunctions.SampleLibUsage;
using Exactly.Once.AzureFunctions.SampleLibUsage.Api;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;

[assembly: WebJobsStartup(typeof(HostStartup))]
namespace Exactly.Once.AzureFunctions.SampleLibUsage
{
    public class HostStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            var endpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
            var primaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");
            var databaseId = "E1Sandbox";

            var client = new CosmosClient(endpointUri, primaryKey);
            builder.Services.AddSingleton(sp => client);
            builder.Services.AddSingleton(sp => new StateStore(client, databaseId));

            builder.AddExactlyOnce(c =>
            {
                c.ConfigureOutbox(o =>
                {
                    o.DatabaseId = databaseId;
                    o.ContainerId = "Outbox";
                    o.RetentionPeriod = TimeSpan.FromSeconds(30);
                });

                c.StateStoreIs<StateStore>();
            });
        }
    }

    class StateStore : IStateStore
    {
        Database database;

        public StateStore(CosmosClient cosmosClient, string databaseId)
        {
            database = cosmosClient.CreateDatabaseIfNotExistsAsync(databaseId).GetAwaiter().GetResult();
        }


        public async Task<(TState, string)> Load<TState>(string stateId) where TState : State, new()
        {
            Container container = await database
                .DefineContainer(typeof(TState).Name, "/id")
                .CreateIfNotExistsAsync();

            using var response = await container.ReadItemStreamAsync(stateId, new PartitionKey(stateId));

            if (response.StatusCode == HttpStatusCode.NotFound)
            {
                return (new TState{Id = stateId}, (string)null);
            }

            if (!response.IsSuccessStatusCode)
            {
                throw new Exception(response.ErrorMessage);
            }

            using var streamReader = new StreamReader(response.Content);

            var content = await streamReader.ReadToEndAsync();

            var state = JsonConvert.DeserializeObject<TState>(content);

            return (state, response.Headers.ETag);
        }

        public async Task<string> Upsert<TState>(string stateId, TState value, string version) where TState : State
        {
            Container container = await database
                .DefineContainer(typeof(TState).Name, "/id")
                .CreateIfNotExistsAsync();

            ItemResponse<TState> response;

            if (version == null)
            {
                response = await container.CreateItemAsync(value, new PartitionKey(stateId));
            }
            else
            {
                response = await container.UpsertItemAsync(
                    value,
                    requestOptions: new ItemRequestOptions
                    {
                        IfMatchEtag = version,
                    });
            }

            if (response.StatusCode == HttpStatusCode.PreconditionFailed)
            {
                throw new Exception("Optimistic concurrency exception on item persist");
            }

            return response.Headers.ETag;
        }
    }
}