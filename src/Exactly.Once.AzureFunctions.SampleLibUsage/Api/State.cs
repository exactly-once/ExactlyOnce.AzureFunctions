using System;
using Newtonsoft.Json;

namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
{
    public abstract class State
    {
        [JsonProperty("id")]
        public string Id { get; set; }

        [JsonProperty("_transactionId")]
        public Guid? TxId { get; set; }
    }
}