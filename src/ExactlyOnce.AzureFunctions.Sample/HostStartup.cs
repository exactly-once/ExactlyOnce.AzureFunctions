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
            builder.AddExactlyOnce(c =>
            {
                c.ConfigureOutbox(o =>
                {
                    o.DatabaseId = "E1Sandbox";
                    o.ContainerId = "Outbox";
                    o.RetentionPeriod = TimeSpan.FromSeconds(30);
                });

                c.UseCosmosClient(() =>
                {
                    var endpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
                    var primaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");

                    return new CosmosClient(endpointUri, primaryKey);
                });
            });
        }
    }
}