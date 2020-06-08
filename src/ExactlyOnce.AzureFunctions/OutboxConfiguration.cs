namespace ExactlyOnce.AzureFunctions
{
    public class OutboxConfiguration
    {
        public string EndpointUri { get; set; }
        public string PrimaryKey { get; set; }
        public string DatabaseId { get; set; }
        public string ContainerId { get; set; } = "Outbox";
    }
}