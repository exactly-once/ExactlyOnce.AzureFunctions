namespace ExactlyOnce.AzureFunctions
{
    public class SendMessage<T> : SideEffect
    {
        public SendMessage()
        {
        }

        public SendMessage(T message)
        {
            Message = message;
        }

        public T Message { get; set; }
    }
}