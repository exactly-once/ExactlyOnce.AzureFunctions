using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions.Sample
{
    public class Envelope
    {
        public Dictionary<string, string> Headers { get; set; }

        public string Content { get; set; }
    }
}