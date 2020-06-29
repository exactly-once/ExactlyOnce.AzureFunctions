using System;
using System.Threading.Tasks;

namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
{
    public interface IOnceExecutor
    {
        IExecutor Once(string requestId);
    }

    public interface IExecutor
    {
        Task<SideEffect[]> On<T>(string stateId, Func<T, SideEffect[]> action) where T : State, new();
    }

    class OnceExecutor : IOnceExecutor
    {
        ExactlyOnceProcessor exactlyOnceProcessor;

        public OnceExecutor(ExactlyOnceProcessor exactlyOnceProcessor)
        {
            this.exactlyOnceProcessor = exactlyOnceProcessor;
        }

        public IExecutor Once(string requestId)
        {
            return new Executor(requestId, exactlyOnceProcessor);
        }
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
            return await exactlyOnceProcessor.Process<TState>(requestId, stateId, action);
        }
    }
}