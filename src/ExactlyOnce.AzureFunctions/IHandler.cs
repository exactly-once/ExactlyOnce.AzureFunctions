using System;

namespace ExactlyOnce.AzureFunctions
{
    public interface IHandler<T>
    {
        Guid Map(T m);
        void Handle(IHandlerContext context, T message);
    }
}