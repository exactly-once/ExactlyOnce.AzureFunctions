namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
{
    public class OutboxConfiguration
    {
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; } = "Outbox";
    }
}