using System;

namespace ExactlyOnce.AzureFunctions.Sample.Domain
{
    class FireAt : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    class StartNewRound : Message
    {
        public Guid GameId { get; set; }
        public int Position { get; set; }
    }

    class Hit : Message
    {
        public Guid GameId { get; set; }
    }

    class Missed : Message
    {
        public Guid GameId { get; set; } 
    }

    abstract class Message
    {
        public Guid Id { get; set; }
    }
}