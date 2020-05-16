using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    interface IExactlyOnce
    {
        Task Process(Guid businessId, Type stateType, Message message, Func<Message, object, Message[]> handle, Func<Message[], Task> publish);
    }
}