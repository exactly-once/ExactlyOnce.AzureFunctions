using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample.Domain;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class HandlerInvoker
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

        static List<Message> InvokeHandler(Type handlerType, Message inputMessage, object state)
        {
            var handler = Activator.CreateInstance(handlerType);
            var handlerContext = new HandlerContext(inputMessage.Id);

            ((dynamic) handler).Data = (dynamic)state;
            ((dynamic) handler).Handle(handlerContext, (dynamic) inputMessage);
            
            return handlerContext.Messages;
        }
    }
}