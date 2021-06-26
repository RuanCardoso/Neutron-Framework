using System.Collections.Concurrent;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    { }
}