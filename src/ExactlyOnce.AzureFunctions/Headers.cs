namespace ExactlyOnce.AzureFunctions
{
    static class Headers
    {
        public const string ConversationId = "ExactlyOnce.ConversationId";
        public const string AuditProcessedMessageId = "ExactlyOnce.Audit.ProcessedMessageId";
        public const string AuditInFlightMessageIds = "ExactlyOnce.Audit.InFlightMessageIds";
    }
}