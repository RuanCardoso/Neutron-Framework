using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeQueue<T> : ConcurrentQueue<T>
    {
        public NeutronSafeQueue()
        {
        }

        public NeutronSafeQueue(IEnumerable<T> collection) : base(collection)
        {
        }
    }
}