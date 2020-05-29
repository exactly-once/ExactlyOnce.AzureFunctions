using System;
using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions
{
    public static class ObjectExtensions
    {
        public static string ToJson(this object message)
        {
            return MessageSerializer.ToJson(
                Guid.NewGuid(), 
                new Dictionary<string, string>(),
                message);
        }
    }
}