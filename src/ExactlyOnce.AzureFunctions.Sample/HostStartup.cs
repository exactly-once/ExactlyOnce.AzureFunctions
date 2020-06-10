using System;
using ExactlyOnce.AzureFunctions.Sample;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(HostStartup))]
namespace ExactlyOnce.AzureFunctions.Sample
{
    public class HostStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExactlyOnce(c =>
            {
                c.AddHandler<ShootingRange>();
                c.AddHandler<LeaderBoard>();

                c.ConfigureRouting(r =>
                {
                    r.ConnectionString = Environment.GetEnvironmentVariable("E1_StorageAccount_ConnectionString");

                    r.AddMessageRoute<Hit>(Destinations.Workflow);
                    r.AddMessageRoute<Missed>(Destinations.Workflow);
                });

                c.ConfigureOutbox(o =>
                {
                    o.DatabaseId = "E1Sandbox";
                    o.EndpointUri = Environment.GetEnvironmentVariable("E1_CosmosDB_EndpointUri");
                    o.PrimaryKey = Environment.GetEnvironmentVariable("E1_CosmosDB_Key");
                });
            });
        }
    }

    public class Destinations
    {
        public const string Workflow = "e1queue";
    }
}