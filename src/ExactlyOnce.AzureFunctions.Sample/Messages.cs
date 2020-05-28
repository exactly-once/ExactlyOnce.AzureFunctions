using System;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class FireAt
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    public class StartNewRound
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    public class Hit
    {
        public Guid GameId { get; set; }
    }

    public class Missed
    {
        public Guid GameId { get; set; } 
    }
}