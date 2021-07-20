using System.Collections.Concurrent;
using System.Collections.Generic;

public class NeutronSafeQueue<T> : ConcurrentQueue<T>
{
    public NeutronSafeQueue()
    {
    }

    public NeutronSafeQueue(IEnumerable<T> collection) : base(collection)
    {
    }
}