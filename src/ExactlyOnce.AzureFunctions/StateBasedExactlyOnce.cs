using System;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace ExactlyOnce.AzureFunctions
{
    class StateBasedExactlyOnce : IExactlyOnce
    {
        ILogger<StateBasedExactlyOnce> logger;
        StateStore stateStore;

        public StateBasedExactlyOnce(ILogger<StateBasedExactlyOnce> logger, StateStore stateStore)
        {
            this.logger = logger;
            this.stateStore = stateStore;
        }

        public async Task Process(Guid businessId, Type stateType, Message message, Func<Message, object, Message[]> handle, Func<Message[], Task> publish)
        {
            var (state, stream, duplicate) = await stateStore.LoadState(stateType, businessId, message.Id);

            logger.LogInformation(
                $"Message:[type={message.GetType().Name}:id={message.Id.ToString("N").Substring(0, 4)}:dup={duplicate}]");

            var outputMessages = handle(message, state);

            if (duplicate == false)
            {
                await stateStore.SaveState(stream, state, message.Id);
            }
           
            await publish(outputMessages);
        }
    }
}