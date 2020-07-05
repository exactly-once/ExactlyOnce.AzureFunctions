using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    public interface IOnceExecutor
    {
        IExecutor Once<TRequest>(string requestId);

        IExecutor Once<TRequest>(Guid requestId);

        Task<TSideEffect> Once<TSideEffect>(string requestId, Func<TSideEffect> action);
    }

    public interface IExecutor
    {
        Task<SideEffect[]> On<T>(string stateId, Func<T, SideEffect[]> action) where T : State, new();
        Task<SideEffect[]> On<T>(Guid stateId, Func<T, SideEffect[]> action) where T : State, new();

        Task<SideEffect> On<T>(Guid stateId, Func<T, SideEffect> action) where T : State, new();
        Task<SideEffect> On<T>(string stateId, Func<T, SideEffect> action) where T : State, new();

        Task On<T>(string stateId, Action<T> action) where T : State, new();
        Task On<T>(Guid stateId, Action<T> action) where T : State, new();
    }

    class OnceExecutor : IOnceExecutor
    {
        ExactlyOnceProcessor exactlyOnceProcessor;

        public OnceExecutor(ExactlyOnceProcessor exactlyOnceProcessor)
        {
            this.exactlyOnceProcessor = exactlyOnceProcessor;
        }

        public IExecutor Once<TRequest>(string requestId)
        {
            var requestTypeAndId = $"{typeof(TRequest).Name}-{requestId}";

            return new Executor(requestTypeAndId, exactlyOnceProcessor);
        }

        public IExecutor Once<TRequest>(Guid requestId)
        {
            return Once<TRequest>(requestId.ToString());
        }

        public async Task<TSideEffect> Once<TSideEffect>(string requestId, Func<TSideEffect> action)
        {
            var sideEffects = await Once<string>(requestId)
                .On<Request>(Guid.Empty, _ => new[] {new SendMessage<TSideEffect>(action()) });

            return ((SendMessage<TSideEffect>) sideEffects[0]).Message;
        }

        public class Request : State {}
    }

    class Executor : IExecutor
    {
        string requestId;
        ExactlyOnceProcessor exactlyOnceProcessor;

        public Executor(string requestId, ExactlyOnceProcessor exactlyOnceProcessor)
        {
            this.requestId = requestId;
            this.exactlyOnceProcessor = exactlyOnceProcessor;
        }

        public async Task<SideEffect[]> On<TState>(string stateId, Func<TState, SideEffect[]> action) where TState : State, new()
        {
            var stateAndRequestId = $"{typeof(TState).Name}-{requestId}";

            return await exactlyOnceProcessor.Process<TState>(stateAndRequestId, stateId, action);
        }

        public Task<SideEffect[]> On<TState>(Guid stateId, Func<TState, SideEffect[]> action) where TState : State, new()
        {
            return On<TState>(stateId.ToString(), action);
        }

        public Task<SideEffect> On<TState>(Guid stateId, Func<TState, SideEffect> action) where TState : State, new()
        {
            return On<TState>(stateId.ToString(), action);
        }

        public async Task<SideEffect> On<TState>(string stateId, Func<TState, SideEffect> action) where TState : State, new()
        {
            var sideEffects = await On<TState>(stateId,s => new []{action(s)});

            return sideEffects[0];
        }

        public Task On<TState>(string stateId, Action<TState> action) where TState : State, new()
        {
            return On<TState>(stateId, s =>
            {
                action(s);
                return new SideEffect[0];
            });
        }

        public Task On<TState>(Guid stateId, Action<TState> action) where TState : State, new()
        {
            return On(stateId.ToString(), action);
        }
    }
}