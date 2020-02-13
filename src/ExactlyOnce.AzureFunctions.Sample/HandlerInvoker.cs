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

        public HandlerInvoker(StateStore stateStore, ILogger<HandlerInvoker> logger)
        {
            this.stateStore = stateStore;
            this.logger = logger;
        }

        public async Task<Message[]> Process(Message message)
        {
            Func<Task<Message[]>> invoke;
            
            if (message is FireAt fireAt)
            {
                invoke = () => Invoke<ShootingRange, ShootingRange.ShootingRangeData>(fireAt.GameId, fireAt);
            } 
            else if (message is StartNewRound moveTarget)
            {
                invoke = () => Invoke<ShootingRange, ShootingRange.ShootingRangeData>(moveTarget.GameId, moveTarget);
            } 
            else if (message is Missed missed)
            {
                invoke = () => Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(missed.GameId, missed);
            } 
            else if (message is Hit hit)
            {
                invoke = () => Invoke<LeaderBoard, LeaderBoard.LeaderBoardData>(hit.GameId, hit);
            }
            else
            {
                throw new Exception($"Unknown message type: {message.GetType().FullName}");
            }

            var outputMessages = await invoke();

            return outputMessages;
        }

        async Task<Message[]> Invoke<THandler, THandlerState>(Guid stateId, Message inputMessage) 
            where THandler : new() 
            where THandlerState : new()
        {
            var messageId = inputMessage.Id;

            var (state, stream, duplicate) = await stateStore.LoadState<THandlerState>(stateId, messageId);

            logger.LogInformation($"Invoking {typeof(THandler).Name}. Message:[type={inputMessage.GetType().Name}:id={messageId.ToString("N").Substring(0,4)}:dup={duplicate}]");
            
            var outputMessages = InvokeHandler<THandler, THandlerState>(inputMessage, state);

            if (duplicate == false)
            { 
                await stateStore.SaveState(stream, state, messageId);
            }

            return outputMessages.ToArray();
        }

        static List<Message> InvokeHandler<THandler, THandlerState>(Message inputMessage, THandlerState state)
            where THandler : new() where THandlerState : new()
        {
            var handler = new THandler();
            var handlerContext = new HandlerContext(inputMessage.Id);

            ((dynamic) handler).Data = state;
            ((dynamic) handler).Handle(handlerContext, (dynamic) inputMessage);
            
            return handlerContext.Messages;
        }
    }
}