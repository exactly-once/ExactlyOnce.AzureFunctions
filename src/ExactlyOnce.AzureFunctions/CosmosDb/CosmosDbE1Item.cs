namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbE1Item
    {
        public string ETag { get; set; }
        public CosmosDbE1Content Item { get; set; }
    }
}