using System;

namespace ExactlyOnce.AzureFunctions.Sample.Domain
{
    class LeaderBoard : Manages<LeaderBoard.LeaderBoardData>, IHandler<Hit>, IHandler<Missed>
    {
        public Guid Map(Hit m) => m.GameId;
        public Guid Map(Missed m) => m.GameId;

        public void Handle(IHandlerContext context, Hit @event)
        {
            Data.NumberOfHits++;
        }

        public void Handle(IHandlerContext context, Missed @event)
        {
            Data.NumberOfMisses++;
        }

        public class LeaderBoardData
        {
            public int NumberOfHits { get; set; }
            public int NumberOfMisses { get; set; }
        }
    }
}