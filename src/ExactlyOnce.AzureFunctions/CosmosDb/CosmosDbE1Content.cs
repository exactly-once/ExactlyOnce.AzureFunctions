using System;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbE1Content
    {
        [JsonProperty("id")]
        public string Id { get; set; }
        public Guid? TransactionId { get; set; }
    }
}