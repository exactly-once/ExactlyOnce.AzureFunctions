using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbOutboxState
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string MessageId { get; set; }

        public Message[] OutputMessages { get; set; }
    }
}