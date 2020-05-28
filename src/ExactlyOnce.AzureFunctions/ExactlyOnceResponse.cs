using System;
using Microsoft.Azure.WebJobs.Description;

namespace ExactlyOnce.AzureFunctions
{
    [Binding]
    [AttributeUsage(AttributeTargets.Parameter | AttributeTargets.ReturnValue)]
    public class ExactlyOnceResponseAttribute : Attribute
    {
        [AutoResolve] public string Headers { get; set; } = "{QueueTrigger}";
    }
}