using System.Collections.Concurrent;
using System.Collections.Generic;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeDictionary<TKey, TValue> : ConcurrentDictionary<TKey, TValue>
    {
        public NeutronSafeDictionary()
        {
        }

        public NeutronSafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection) : base(collection)
        {
        }

        public NeutronSafeDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
        {
        }

        public NeutronSafeDictionary(IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(collection, comparer)
        {
        }

        public NeutronSafeDictionary(System.Int32 concurrencyLevel, System.Int32 capacity) : base(concurrencyLevel, capacity)
        {
        }

        public NeutronSafeDictionary(System.Int32 concurrencyLevel, IEnumerable<KeyValuePair<TKey, TValue>> collection, IEqualityComparer<TKey> comparer) : base(concurrencyLevel, collection, comparer)
        {
        }

        public NeutronSafeDictionary(System.Int32 concurrencyLevel, System.Int32 capacity, IEqualityComparer<TKey> comparer) : base(concurrencyLevel, capacity, comparer)
        {
        }
    }
}