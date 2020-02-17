﻿using System;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample;

namespace ExactlyOnce.AzureFunctions.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var sender = ExactlyOnceServiceCollectionExtensions.CreateMessageSender();

            var i = 0;
            while (true)
            {
                System.Console.WriteLine("Press <enter> to send messages...");
                
                while(System.Console.ReadKey().Key != ConsoleKey.Enter){}

                var message = new StartNewRound
                {
                    Id = Guid.NewGuid(),
                    GameId = Guid.Empty, 
                    Position = 10 + (i++)
                };

                await sender.Publish(new Message[]{message});
            }
        }
    }
}
