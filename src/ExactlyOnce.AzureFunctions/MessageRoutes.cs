using System;
using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions
{
    public class MessageRoutes
    {
        public Dictionary<Type, string> Routes = new Dictionary<Type, string>();
    }
}