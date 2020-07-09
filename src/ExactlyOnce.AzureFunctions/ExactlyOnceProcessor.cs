using Newtonsoft.Json;
using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    public class ExactlyOnceProcessor
    {
        OutboxStore outboxStore;
        IStateStore stateStore;

        public ExactlyOnceProcessor(OutboxStore outboxStore, IStateStore stateStore)
        {
            this.outboxStore = outboxStore;
            this.stateStore = stateStore;
        }

        public async Task<TSideEffect> Process<TState, TSideEffect>(string requestId, string stateId,
            Func<TState, TSideEffect> handle) where TState : State, new()
        {
            var (state, version) = await stateStore.Load<TState>(stateId);

            if (state.TxId != null)
            {
                await FinishTransaction(stateId, state, version);
            }

            var outboxState = await outboxStore.Get(requestId);

            if (outboxState == null)
            {
                var sideEffect = handle(state);

                state.TxId = Guid.NewGuid();

                outboxState = new OutboxItem
                {
                    Id = state.TxId.ToString(),
                    RequestId = requestId,
                    SideEffect = JsonConvert.SerializeObject(sideEffect)
                };

                await outboxStore.Store(outboxState);

                string nextVersion;

                try
                {
                    nextVersion = await stateStore.Upsert(stateId, state, version);
                }
                catch
                {
                    await outboxStore.Delete(outboxState.Id);
                    throw;
                }

                await FinishTransaction(stateId, state, nextVersion);

                return sideEffect;
            }

            return JsonConvert.DeserializeObject<TSideEffect>(outboxState.SideEffect);
        }

        async Task FinishTransaction<TState>(string stateId, TState state, string version) where TState : State
        {
            if (state.TxId.HasValue == false)
            {
                throw new InvalidOperationException($"No pending transaction for state id {stateId}.");
            }

            await outboxStore.Commit(state.TxId.Value.ToString());

            state.TxId = null;
            await stateStore.Upsert(stateId, state, version);
        }
    }
}