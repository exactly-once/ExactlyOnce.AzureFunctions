using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ExactlyOnce.AzureFunctions.Sample.Domain;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class HandlerInvoker
    {
        private readonly StateStore stateStore;

        public HandlerInvoker(StateStore stateStore)
        {
            this.stateStore = stateStore;
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