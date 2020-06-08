using System;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample;

namespace ExactlyOnce.AzureFunctions.Console
{
    class Program
    {
        static async Task Main(string[] args)
        {
            var routing = new RoutingConfiguration
            {
                ConnectionString = "UseDevelopmentStorage=true;"
            };
            routing.AddMessageRoute<StartNewRound>("test");

            var sender = new MessageSender(new QueueProvider(routing), routing);

            var i = 0;
            while (true)
            {
                System.Console.WriteLine("Press <enter> to send messages...");
                
                while(System.Console.ReadKey().Key != ConsoleKey.Enter){}

                var message = new StartNewRound
                {
                    GameId = Guid.Empty, 
                    Position = 10 + (i++)
                };

                await sender.Publish(Guid.NewGuid(), message);
            }
        }
    }
}
