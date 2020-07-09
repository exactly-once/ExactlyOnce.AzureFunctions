using Newtonsoft.Json;
using System;

namespace ExactlyOnce.AzureFunctions
{
    public abstract class State
    {
        [JsonProperty("id")] public string Id { get; set; }

        [JsonProperty("_transactionId")] public Guid? TxId { get; set; }
    }
}