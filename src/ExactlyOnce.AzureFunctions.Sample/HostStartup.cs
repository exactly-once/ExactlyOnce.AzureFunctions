using System;
using ExactlyOnce.AzureFunctions.Sample;
using Microsoft.Azure.Cosmos;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;
using Microsoft.Extensions.DependencyInjection;

[assembly: WebJobsStartup(typeof(HostStartup))]
namespace ExactlyOnce.AzureFunctions.Sample
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

    public class Destinations
    {
        public const string Workflow = "e1queue";
    }
}