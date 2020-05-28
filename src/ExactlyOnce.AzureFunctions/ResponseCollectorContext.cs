namespace ExactlyOnce.AzureFunctions
{
    class ResponseCollectorContext
    {
        public ExactlyOnceResponseAttribute ResolvedAttribute { get; set; }

        public MessageSender Sender { get; set; }
    }
}