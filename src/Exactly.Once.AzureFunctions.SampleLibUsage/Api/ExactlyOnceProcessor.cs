using System;
using System.Linq;
using System.Threading.Tasks;
using Newtonsoft.Json;

namespace Exactly.Once.AzureFunctions.SampleLibUsage.Api
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

        public async Task<SideEffect[]> Process<TState>(string requestId, string stateId,
            Func<TState, SideEffect[]> handle) where TState : State, new()
        {
            var (state, version) = await stateStore.Load<TState>(stateId);

            if (state.TxId != null)
            {
                await FinishTransaction(stateId, state, version);
            }

            var outboxState = await outboxStore.Get(requestId);

            if (outboxState == null)
            {
                var sideEffects = handle(state);

                state.TxId = Guid.NewGuid();

                outboxState = new OutboxItem
                {
                    Id = state.TxId.ToString(),
                    RequestId = requestId,
                    SideEffects = WrapSideEffects(sideEffects),
                };

                await outboxStore.Store(outboxState);

                var nextVersion = await stateStore.Upsert(stateId, state, version);

                await FinishTransaction(stateId, state, nextVersion);

                return sideEffects;
            }

            return UnWrapSideEffects(outboxState.SideEffects);
        }

        SideEffect[] UnWrapSideEffects(SideEffectWrapper[] wrappedSideEffects)
        {
            return wrappedSideEffects
                .Select(se => (SideEffect) JsonConvert.DeserializeObject(se.Content, Type.GetType(se.Type))).ToArray();
        }

        SideEffectWrapper[] WrapSideEffects(SideEffect[] sideEffects)
        {
            return sideEffects.Select(se => new SideEffectWrapper
            {
                Type = se.GetType().FullName,
                Content = JsonConvert.SerializeObject(se)
            }).ToArray();
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