using System;

namespace ExactlyOnce.AzureFunctions
{
    public abstract class Message
    {
        public Guid Id { get; set; }
    }
}