using System;
using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions
{
    class HandlerInvoker
    {
        HandlersMap handlersMap;
        public HandlerInvoker(HandlersMap handlersMap)
        {
            this.handlersMap = handlersMap;
        }

        public HandlerDescriptor GetHandler(Message message)
        {
            return handlersMap.ForMessage(message.GetType());
        }

        public Message[] Process(Message message, HandlerDescriptor handler, object state)
        {
            var outputMessages = InvokeHandler(handler.HandlerType, message, state);

            return outputMessages.ToArray();
        }

        static List<Message> InvokeHandler(Type handlerType, Message message, object state)
        {
            var handler = Activator.CreateInstance(handlerType);
            var handlerContext = new HandlerContext(message.Id);

            var dataPropertyName = nameof(Manages<object>.Data);
            var dataProperty = handlerType.GetProperty(dataPropertyName);
            if (dataProperty == null)
            {
                throw new Exception($"Error calling handler. Can't find ${dataPropertyName}' property");
            }

            dataProperty?.SetValue(handler, state);

            var handleMethodName = nameof(IHandler<object>.Handle);
            var handleMethod =
                handlerType.GetMethod(handleMethodName, new[] {handlerContext.GetType(), message.GetType()});
            if (handleMethod == null)
            {
                throw new Exception($"Error calling handler. Can't find ${handleMethodName}' method");
            }

            handleMethod?.Invoke(handler, new object[] {handlerContext, message});

            return handlerContext.Messages;
        }
    }
}