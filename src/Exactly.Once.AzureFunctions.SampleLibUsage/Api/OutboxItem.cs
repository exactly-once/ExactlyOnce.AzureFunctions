using Newtonsoft.Json;

namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
{
    public class OutboxItem
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("requestId")]
        public string RequestId { get; set; }

        [JsonProperty("sideEffects")]
        public SideEffectWrapper[] SideEffects { get; set; }

        [JsonProperty(PropertyName = "ttl", NullValueHandling = NullValueHandling.Ignore)]
        public int? TimeToLiveSeconds { get; set; }
    }
}