using System;
using System.Threading.Tasks;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class LeaseManager
    {
        public Task<Lease> AcquireLease(Guid businessId)
        {
            return Task.FromResult(new Lease());
        }
    }

    public class Lease
    {
        public Task Release()
        {
            return Task.CompletedTask;
        }
    }
}