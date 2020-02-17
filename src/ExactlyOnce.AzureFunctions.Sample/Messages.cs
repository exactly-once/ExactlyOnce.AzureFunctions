using System;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class FireAt : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    public class StartNewRound : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    public class Hit : Message
    {
        public Guid GameId { get; set; }
    }

    public class Missed : Message
    {
        public Guid GameId { get; set; } 
    }
}