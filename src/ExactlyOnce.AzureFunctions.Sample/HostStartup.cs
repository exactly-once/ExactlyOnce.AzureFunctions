using ExactlyOnce.AzureFunctions;
using ExactlyOnce.AzureFunctions.Sample.Domain;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(HostStartup))]
public class HostStartup : IWebJobsStartup
{
    public void Configure(IWebJobsBuilder builder)
    {
        builder.Services.AddExactlyOnce()
            .AddHandler<ShootingRange>()
            .AddHandler<LeaderBoard>();
    }
}