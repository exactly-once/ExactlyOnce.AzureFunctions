using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Azure.Storage;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions
{
    public class HandlerInvoker
    {
        private readonly StateStore stateStore;
        private readonly ILogger<HandlerInvoker> logger;
        private readonly HandlersMap handlersMap;

        public HandlerInvoker(StateStore stateStore, ILogger<HandlerInvoker> logger, HandlersMap handlersMap)
        {
            this.stateStore = stateStore;
            this.logger = logger;
            this.handlersMap = handlersMap;
        }

        public async Task<Message[]> Process(Message message)
        {
            var handler = handlersMap.ForMessage(message.GetType());

            var businessId = handler.GetBusinessId(message);

            var (state, stream, duplicate) = await stateStore.LoadState(handler.DataType, businessId, message.Id);

            logger.LogInformation($"Invoking {handler.HandlerType.Name}. Message:[type={message.GetType().Name}:id={message.Id.ToString("N").Substring(0,4)}:dup={duplicate}]");

            var outputMessages = InvokeHandler(handler.HandlerType, message, state);

            if (duplicate == false)
            { 
                await stateStore.SaveState(stream, state, message.Id);
            }

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
            var handleMethod = handlerType.GetMethod(handleMethodName, new[] {handlerContext.GetType(), message.GetType()});
            if (handleMethod == null)
            {
                throw new Exception($"Error calling handler. Can't find ${handleMethodName}' method");
            }
            
            handleMethod?.Invoke(handler, new object []{handlerContext, message});
            
            return handlerContext.Messages;
        }
    }
}