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

                c.AddMessageRoute<Hit>(Destinations.Workflow);
                c.AddMessageRoute<Missed>(Destinations.Workflow);
            });
        }
    }

    public class Destinations
    {
        public const string Workflow = "test";
    }
}