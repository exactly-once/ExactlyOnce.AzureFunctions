using System;
using ExactlyOnce.AzureFunctions.CosmosDb;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class LeaderBoard : Manages<LeaderBoard.LeaderBoardData>, IHandler<Hit>, IHandler<Missed>
    {
        public Guid Map(Hit m) => m.GameId;
        public Guid Map(Missed m) => m.GameId;


        public void Handle(HandlerContext context, Hit @event)
        {
            Console.WriteLine($"##########: Hit saved {@event.GameId}");
            Data.NumberOfHits++;
        }

        public void Handle(HandlerContext context, Missed @event)
        {
            Data.NumberOfMisses++;
        }

        public class LeaderBoardData : CosmosDbE1Content
        {
            public int NumberOfHits { get; set; }
            public int NumberOfMisses { get; set; }
        }
    }
}