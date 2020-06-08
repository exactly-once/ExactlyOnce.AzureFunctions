using System;

namespace ExactlyOnce.AzureFunctions
{
    public interface IHandler<T>
    {
        Guid Map(T m);
        void Handle(HandlerContext context, T message);
    }
}