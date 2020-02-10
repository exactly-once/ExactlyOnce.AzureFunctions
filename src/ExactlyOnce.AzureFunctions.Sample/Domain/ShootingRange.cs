namespace ExactlyOnce.AzureFunctions.Sample.Domain
{
    class ShootingRange
    {
        public const int MaxAttemptsInARound = 2;

        public ShootingRangeData Data { get; set; }

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

        public class ShootingRangeData
        {
            public int TargetPosition { get; set; }
            public int NumberOfAttempts { get; set; }
        }
    }
}