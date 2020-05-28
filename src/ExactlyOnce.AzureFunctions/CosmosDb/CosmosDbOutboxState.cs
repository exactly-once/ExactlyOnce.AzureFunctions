using System;
using Newtonsoft.Json;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbOutboxState
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        public string MessageId { get; set; }

        [JsonIgnore]
        public object[] OutputMessages { get; set; }

        [JsonIgnore]
        public Guid[] OutputMessagesIds { get; set; }

        public string[] OutputMessagesText { get; set; }
    }
}