using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbOutboxState
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string MessageId { get; set; }

        [JsonIgnore]
        public Message[] OutputMessages { get; set; }

        public string[] OutputMessagesText { get; set; }
    }
}