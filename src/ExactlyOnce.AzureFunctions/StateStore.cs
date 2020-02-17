using System;
using System.Threading.Tasks;
using Microsoft.Azure.Cosmos.Table;
using Newtonsoft.Json;
using Streamstone;

namespace ExactlyOnce.AzureFunctions
{
    class StateStore
    {
        readonly CloudTable table;

        public StateStore(CloudTable table)
        {
            this.table = table;
        }

        public async Task<(object, Stream, bool)> LoadState(Type stateType, Guid stateId, Guid messageId)
        {
            var streamId = $"{stateType.Name}-{stateId}";
            var partition = new Partition(table, streamId);

            var state = Activator.CreateInstance(stateType);

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
                var nextState = DeserializeEvent(stateType, properties);

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

        object DeserializeEvent(Type stateType, EventProperties properties)
        {
            var data = properties["Data"].StringValue;
            var state = JsonConvert.DeserializeObject(data, stateType);

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