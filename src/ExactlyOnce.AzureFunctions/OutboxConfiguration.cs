using System;

namespace ExactlyOnce.AzureFunctions
{
    public class OutboxConfiguration
    {
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; } = "Outbox";
        public TimeSpan RetentionPeriod { get; set; }
    }
}