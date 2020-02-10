namespace ExactlyOnce.AzureFunctions.Sample.Domain
{
    class LeaderBoard
    {
        public LeaderBoardData Data { get; set; }

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