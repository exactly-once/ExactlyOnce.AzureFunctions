using System;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class ShootingRange : Manages<ShootingRange.ShootingRangeData>, IHandler<FireAt>, IHandler<StartNewRound>
    {
        public const int MaxAttemptsInARound = 2;

        public void Handle(IHandlerContext context, FireAt command)
        {
            if (Data.TargetPosition == command.Position)
            {
                context.Publish(new Hit
                {
                    Id = context.NewGuid(),
                    GameId = command.GameId
                });
            }
            else
            {
                context.Publish(new Missed
                {
                    Id = context.NewGuid(),
                    GameId = command.GameId
                });
            }

            if (Data.NumberOfAttempts + 1 >= MaxAttemptsInARound)
            {
                Data.NumberOfAttempts = 0;
                Data.TargetPosition = context.Random.Next(0, 100);
            }
            else
            {
                Data.NumberOfAttempts++;
            }
        }

        public void Handle(IHandlerContext context, StartNewRound command)
        {
            Data.NumberOfAttempts = 0;
            Data.TargetPosition = command.Position;
        }

        public Guid Map(FireAt m) => m.GameId;
        public Guid Map(StartNewRound m) => m.GameId;

        public class ShootingRangeData
        {
            public int TargetPosition { get; set; }
            public int NumberOfAttempts { get; set; }
        }
    }
}