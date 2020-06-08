using System;
using System.Collections.Generic;

namespace ExactlyOnce.AzureFunctions
{
    public class RoutingConfiguration
    {
        internal Dictionary<Type, string> Routes = new Dictionary<Type, string>();

        public string ConnectionString { get; set; }
        public void AddMessageRoute<T>(string destination)
        {
            Routes.Add(typeof(T), destination);
        }
    }
}