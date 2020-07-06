using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    public interface IOnceExecutor
    {
        IStateSelector Once<TRequest>(string requestId);

        IStateSelector Once<TRequest>(Guid requestId);

        Task<TSideEffect> Once<TSideEffect>(string requestId, Func<TSideEffect> action);
    }

    public interface IStateSelector
    {
        IOutputInvoker<T> On<T>(string stateId) where T : State, new();
        IOutputInvoker<T> On<T>(Guid stateId) where T : State, new();

        Task On<T>(string stateId, Action<T> action) where T : State, new();
        Task On<T>(Guid stateId, Action<T> action) where T : State, new();
    }

    public interface IOutputInvoker<TState>
    {
        Task<TSideEffect> WithOutput<TSideEffect>(Func<TState, TSideEffect> action);
    }

    class OnceExecutor : IOnceExecutor
    {
        ExactlyOnceProcessor exactlyOnceProcessor;

        public OnceExecutor(ExactlyOnceProcessor exactlyOnceProcessor)
        {
            this.exactlyOnceProcessor = exactlyOnceProcessor;
        }

        public IStateSelector Once<TRequest>(string requestId)
        {
            var requestTypeAndId = $"{typeof(TRequest).Name}-{requestId}";

            return new StateSelector(requestTypeAndId, exactlyOnceProcessor);
        }

        public IStateSelector Once<TRequest>(Guid requestId)
        {
            return Once<TRequest>(requestId.ToString());
        }

        public Task<TSideEffect> Once<TSideEffect>(string requestId, Func<TSideEffect> action)
        {
            return Once<string>(requestId)
                .On<Request>(Guid.Empty)
                .WithOutput(_ => action());
        }

        public class Request : State
        {
        }
    }

    class StateSelector : IStateSelector
    {
        ExactlyOnceProcessor exactlyOnceProcessor;
        string requestId;

        public StateSelector(string requestId, ExactlyOnceProcessor exactlyOnceProcessor)
        {
            this.requestId = requestId;
            this.exactlyOnceProcessor = exactlyOnceProcessor;
        }

        public IOutputInvoker<TState> On<TState>(string stateId) where TState : State, new()
        {
            var stateAndRequestId = $"{typeof(TState).Name}-{requestId}";

            return new OutputInvoker<TState>(stateAndRequestId, stateId, exactlyOnceProcessor);
        }

        public IOutputInvoker<TState> On<TState>(Guid stateId) where TState : State, new()
        {
            return On<TState>(stateId.ToString());
        }

        public Task On<TState>(string stateId, Action<TState> action) where TState : State, new()
        {
            return On<TState>(stateId).WithOutput(s =>
            {
                action(s);
                return (string) null;
            });
        }

        public Task On<TState>(Guid stateId, Action<TState> action) where TState : State, new()
        {
            return On(stateId.ToString(), action);
        }
    }

    class OutputInvoker<TState> : IOutputInvoker<TState> where TState : State, new()
    {
        ExactlyOnceProcessor processor;
        string requestId;
        string stateId;

        public OutputInvoker(string requestId, string stateId, ExactlyOnceProcessor processor)
        {
            this.requestId = requestId;
            this.stateId = stateId;
            this.processor = processor;
        }

        public Task<TSideEffect> WithOutput<TSideEffect>(Func<TState, TSideEffect> action)
        {
            return processor.Process(requestId, stateId, action);
        }
    }
}