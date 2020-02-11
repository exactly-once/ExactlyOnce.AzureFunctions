using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Streamstone;

namespace ExactlyOnce.AzureFunctions.Sample
{
    class StateStore
    {
        readonly CloudTable table;

        public StateStore(CloudTable table)
        {
            this.table = table;
        }

        public async Task<(THandlerState, Stream, bool)> LoadState<THandlerState>(Guid stateId, Guid messageId) where THandlerState : new()
        {
            var streamId = $"{typeof(THandlerState).Name}-{stateId}";
            var partition = new Partition(table, streamId);

            var state = new THandlerState();

            var existent = await Stream.TryOpenAsync(partition);

            if (existent.Found == false)
            {
                var createdStream = await Stream.ProvisionAsync(partition);

                return (state, createdStream, false);
            }

            var isDuplicate = false;

            var stream = await ReadStream(partition, properties =>
            {
                var mId = properties["MessageId"].GuidValue;
                var nextState = DeserializeEvent<THandlerState>(properties);

                if (mId == messageId)
                {
                    isDuplicate = true;
                } 
                else
                {
                    state = nextState;
                }

                return isDuplicate;
            });

            return (state, stream, isDuplicate);
        }

        THandlerState DeserializeEvent<THandlerState>(EventProperties properties)
        {
            var data = properties["Data"].StringValue;
            var state = JsonConvert.DeserializeObject<THandlerState>(data);

            return state;
        }

        static async Task<Stream> ReadStream(Partition partition, Func<EventProperties, bool> process)
        {
            StreamSlice<EventProperties> slice;

            var sliceStart = 1;

            do
            {
                slice = await Stream.ReadAsync(partition, sliceStart, sliceSize: 1);

                foreach (var @event in slice.Events)
                {
                    if (process(@event)) break;
                }

                sliceStart += slice.Events.Length;
            } 
            while (slice.HasEvents);

            return slice.Stream;
        }

        public Task SaveState(Stream stream, object stateVersion, Guid messageId)
        {
            var eventId = EventId.From(Guid.NewGuid());

            var properties = EventProperties.From(new
            {
                MessageId = messageId,
                Data = JsonConvert.SerializeObject(stateVersion)
            });

            return Stream.WriteAsync(stream, new EventData(eventId, properties));
        }
    }
}