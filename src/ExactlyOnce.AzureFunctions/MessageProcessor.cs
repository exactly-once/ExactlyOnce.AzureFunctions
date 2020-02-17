using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    public class MessageProcessor
    {
        HandlerInvoker handlerInvoker;
        MessageSender sender;

        public MessageProcessor(HandlerInvoker handlerInvoker, MessageSender sender)
        {
            this.handlerInvoker = handlerInvoker;
            this.sender = sender;
        }

        public async Task Process(CloudQueueMessage queueItem)
        {
            var (headers, message) = Serializer.Deserialize(queueItem.AsBytes);

            var outputMessages = await handlerInvoker.Process(message as Message);

            //TODO: recreate runId logic
            //var runId = headers["Message.RunId"];

            //messageProcessed(runId, message, outputMessages);

            await sender.Publish(outputMessages, null);
        }
    }
}