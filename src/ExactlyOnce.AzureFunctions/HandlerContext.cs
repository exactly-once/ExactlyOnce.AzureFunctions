using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions
{
    public class HandlerContext
    {
        public List<object> Messages { get; set; } = new List<object>();

        public void Send(object message)
        {
            Messages.Add(message);
        }
    }
}