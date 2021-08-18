using System.Collections.Concurrent;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeQueueNonAlloc<T> : ConcurrentQueue<T>
    {

    }
}