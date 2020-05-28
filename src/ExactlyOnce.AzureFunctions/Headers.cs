namespace ExactlyOnce.AzureFunctions
{
    static class Headers
    {
        public const string MessageId = "exactlyOnce.id";
        public const string MessageType = "exactlyOnce.messageType";
        public const string ConversationId = "exactlyOnce.conversationId";
        public const string AuditProcessedMessageId = "exactlyOnce.audit.processedMessageId";
        public const string AuditInFlightMessageIds = "exactlyOnce.audit.inFlightMessageIds";
    }
}