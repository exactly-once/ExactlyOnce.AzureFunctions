using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class CosmosDbExactlyOnce : IExactlyOnce
    {
        CosmosDbOutbox outbox;
        CosmosDbStateStore stateStore;

        public CosmosDbExactlyOnce(CosmosDbOutbox outbox, CosmosDbStateStore stateStore)
        {
            this.outbox = outbox;
            this.stateStore = stateStore;
        }

        public async Task Process(Guid businessId, Type stateType, Message message,
            Func<Message, object, Message[]> handle, Func<Message[], Task> publish)
        {
            var state = await LoadState(businessId, stateType);

            if (state.Item.TransactionId != null)
            {
                await FinishTransaction(state);
            }

            var outboxState = await outbox.Get(message.Id);

            if (outboxState == null)
            {
                var outputMessages = handle(message, state.Item);

                state.Item.TransactionId = Guid.NewGuid();

                outboxState = new CosmosDbOutboxState
                {
                    Id = state.Item.TransactionId.ToString(),
                    MessageId = message.Id.ToString(),
                    OutputMessages = outputMessages
                };

                await outbox.Store(outboxState);

                await stateStore.Persist(state);

                await FinishTransaction(state);
            }
            
            await publish(outboxState.OutputMessages);
        }

        async Task FinishTransaction(CosmosDbE1Item state)
        {
            if (state.Item.TransactionId.HasValue == false)
            {
                throw new InvalidOperationException($"No pending transaction for state id {state.Item.Id}.");
            }

            await outbox.Commit(state.Item.TransactionId.Value);

            state.Item.TransactionId = null;
            await stateStore.Persist(state);
        }

        async Task<CosmosDbE1Item> LoadState(Guid businessId, Type stateType)
        {
            var state = await stateStore.Load(businessId, stateType);

            if (state != null)
            {
                return state;
            }

            var item = (CosmosDbE1Content) Activator.CreateInstance(stateType);

            item.Id = businessId.ToString();

            return new CosmosDbE1Item {Item = item};
        }

        public async Task Initialize()
        {
            await outbox.Initialize();
            await stateStore.Initialize();
        }
    }
}