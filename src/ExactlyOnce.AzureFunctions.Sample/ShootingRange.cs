using System;
using ExactlyOnce.AzureFunctions.CosmosDb;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class ShootingRange : Manages<ShootingRange.ShootingRangeData>, IHandler<FireAt>, IHandler<StartNewRound>
    {
        public void Handle(HandlerContext context, FireAt command)
        {
            if (Data.TargetPosition == command.Position)
            {
                context.Send(new Hit
                {
                    GameId = command.GameId
                });
            }
            else
            {
                context.Send(new Missed
                {
                    GameId = command.GameId
                });
            }
            
            Data.NumberOfAttempts++;
        }

        public void Handle(HandlerContext context, StartNewRound command)
        {
            Data.NumberOfAttempts = 0;
            Data.TargetPosition = command.Position;
        }

        public Guid Map(FireAt m) => m.GameId;
        public Guid Map(StartNewRound m) => m.GameId;

        public class ShootingRangeData : CosmosDbE1Content
        {
            public int TargetPosition { get; set; }
            public int NumberOfAttempts { get; set; }
        }
    }
}