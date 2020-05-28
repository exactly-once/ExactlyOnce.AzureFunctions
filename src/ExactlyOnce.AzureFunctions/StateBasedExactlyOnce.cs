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

        public async Task Process(Guid messageId, Guid stateId, Type stateType, object message, Func<object, object, object[]> handle, Func<Guid, object, Task> publish)
        {
            var (state, stream, duplicate) = await stateStore.LoadState(stateType, stateId, messageId);

            logger.LogInformation(
                $"Message:[type={message.GetType().Name}:id={messageId.ToString("N").Substring(0, 4)}:dup={duplicate}]");

            var outputMessages = handle(message, state);

            if (duplicate == false)
            {
                await stateStore.SaveState(stream, state, messageId);
            }

            throw new NotImplementedException("We need to preserve output message id");
            //await publish(outputMessages);
        }
    }
}