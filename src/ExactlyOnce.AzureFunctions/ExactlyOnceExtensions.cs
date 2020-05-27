using System;
using System.Collections.Generic;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Description;
using Microsoft.Azure.WebJobs.Host.Bindings;
using Microsoft.Azure.WebJobs.Host.Config;

namespace ExactlyOnce.AzureFunctions
{
    [Extension("ExactlyOnce")]
    public class ExactlyOnceExtensions : IExtensionConfigProvider
    {
        public void Initialize(ExtensionConfigContext context)
        {
            var rule = context.AddBindingRule<ExactlyOnceResponseAttribute>();            
 
            rule.BindToCollector<ResponseCollectorOpenType>(typeof(ResponseCollectorConverter<>), this);     
        }
 
        class ResponseCollectorOpenType : OpenType.Poco
        {
            public override bool IsMatch(Type type, OpenTypeMatchContext context)
            {
                if (type.IsGenericType
                    && type.GetGenericTypeDefinition() == typeof(IEnumerable<>))
                {
                    return false;
                }

                if (type.FullName == "System.Object")
                {
                    return true;
                }

                return base.IsMatch(type, context);
            }
        }

        public ResponseCollectorContext CreateContext(ExactlyOnceResponseAttribute attribute)
        {
            return new ResponseCollectorContext{ ResolvedAttribute = attribute};
        }
    }
}