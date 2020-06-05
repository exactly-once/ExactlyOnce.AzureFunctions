﻿using ExactlyOnce.AzureFunctions.Sample.Cart;
using ExactlyOnce.AzureFunctions;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Hosting;

[assembly: WebJobsStartup(typeof(HostStartup))]
namespace ExactlyOnce.AzureFunctions.Sample.Cart
{
    public class HostStartup : IWebJobsStartup
    {
        public void Configure(IWebJobsBuilder builder)
        {
            builder.AddExactlyOnce(c =>
            {
                c.AddHandler<OrderWorkflow>();
            });
        }
    }
}