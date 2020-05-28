using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions
{
    interface IExactlyOnce
    {
        Task Process(Guid messageId, Guid stateId, Type stateType, object message, Func<object, object, object[]> handle, Func<Guid, object, Task> publish);
    }
}