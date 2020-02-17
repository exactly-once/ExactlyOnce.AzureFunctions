using System;

namespace ExactlyOnce.AzureFunctions
{
    public class HandlerDescriptor
    {
        public Guid GetBusinessId(Message message)
        {
            var handler = Activator.CreateInstance(HandlerType);

            var mapMethod = HandlerType.GetMethod(nameof(IHandler<object>.Map), new [] {message.GetType()});

            var businessId = mapMethod?.Invoke(handler, new object[] {message}) as Guid?;

            if (businessId == null)
            {
                throw new Exception($"Business id mapping failure for message ${message.GetType().Name}, handler ${HandlerType.Name}");
            }

            return businessId.Value;
        }

        public Type HandlerType { get; set; }
        public Type DataType { get; set; }
    }
}