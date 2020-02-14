using System;
using ExactlyOnce.AzureFunctions.Sample.Domain;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class HandlerDescriptor
    {
        public Guid GetBusinessId(Message message)
        {
            var handler = Activator.CreateInstance(HandlerType);

            return ((dynamic) handler).Map((dynamic) message);
        }

        public Type HandlerType { get; set; }
        public Type DataType { get; set; }
    }
}