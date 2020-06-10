using System;
using System.Collections.Concurrent;
using System.Threading;

namespace ExactlyOnce.AzureFunctions.CosmosDb
{
    public class InMemoryLockManager
    {
        /* In memory locking with SemaphoreSlim for now
         In the future distributed lease-locks with marker documents as lock
        */

        ConcurrentDictionary<Guid, SemaphoreSlim> semaphores = new ConcurrentDictionary<Guid, SemaphoreSlim>();

        public SemaphoreSlim GetSemaphore(Guid businessId)
        {
            if (semaphores.TryGetValue(businessId, out var semaphore))
            {
                return semaphore;
            }

            var newSemaphore = new SemaphoreSlim(1);

            if (semaphores.TryAdd(businessId, newSemaphore))
            {
                return newSemaphore;
            }

            newSemaphore.Dispose();

            return semaphores[businessId];
        }
    }
}