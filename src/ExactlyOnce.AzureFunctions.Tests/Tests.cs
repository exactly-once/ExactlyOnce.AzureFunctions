using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample;
using NUnit.Framework;

namespace ExactlyOnce.AzureFunctions.Tests
{
    public class Tests
    {
        [Test]
        public async Task SendAMessage()
        {
            var sender = ExactlyOnceServiceCollectionExtensions.CreateMessageSender();

            await sender.Publish(new []{new StartNewRound{Id=Guid.NewGuid(), GameId = Guid.Empty, Position = 10}});
            await sender.Publish(new []{new FireAt{Id = Guid.NewGuid(), GameId = Guid.Empty, Position = 10}});

            Assert.Pass();
        }
    }
}