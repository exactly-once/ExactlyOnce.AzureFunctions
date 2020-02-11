using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample;
using ExactlyOnce.AzureFunctions.Sample.Domain;
using NUnit.Framework;

namespace ExactlyOnce.AzureFunctions.Tests
{
    public class Tests
    {
        [Test]
        public async Task SendAMessage()
        {
            var sender = Startup.CreateMessageSender();

            await sender.Publish(new []{new StartNewRound{GameId = Guid.Empty, Position = 10}});
            await sender.Publish(new []{new FireAt{GameId = Guid.Empty, Position = 10}});

            Assert.Pass();
        }
    }
}