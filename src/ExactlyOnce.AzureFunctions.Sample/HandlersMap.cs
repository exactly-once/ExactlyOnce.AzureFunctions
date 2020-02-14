using System;
using System.Collections.Generic;
using System.Linq;
using ExactlyOnce.AzureFunctions.Sample.Domain;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class HandlersMap
    {
        readonly Dictionary<Type, HandlerDescriptor> messageToHandler = new Dictionary<Type, HandlerDescriptor>();

        public void Initialize(Type[] handlers)
        {
            foreach (var handler in handlers)
            {
                var implementsManages = handler.BaseType?.GetGenericTypeDefinition() == typeof(Manages<>);

                if (implementsManages == false)
                {
                    throw new ArgumentException($"{handler.Name} is not not a valid handler. A handler needs to implement Manages<T>.");
                }

                var messagesHandled = handler.GetInterfaces()
                    .Where(i => i.GetGenericTypeDefinition() == typeof(IHandler<>))
                    .Select(i => i.GetGenericArguments()[0]);

                var handlerDescriptor = new HandlerDescriptor
                {
                    DataType = handler.BaseType?.GetGenericArguments()[0],
                    HandlerType = handler
                };

                messagesHandled.ToList().ForEach(m => messageToHandler.Add(m, handlerDescriptor));
            }
        }

        public HandlerDescriptor ForMessage(Type messageType)
        {
            return messageToHandler[messageType];
        }
    }
}