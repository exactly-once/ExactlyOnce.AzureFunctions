using System.Threading.Tasks;
using Microsoft.WindowsAzure.Storage.Queue;

namespace ExactlyOnce.AzureFunctions
{
    public interface IMessageProcessor
    {
        Task Process(CloudQueueMessage queueItem);
    }
}