using System.Collections.Generic;

namespace NeutronNetwork.Wrappers
{
    public class NeutronQueue<T> : Queue<T>
    {
        public NeutronQueue()
        {
        }

        public NeutronQueue(IEnumerable<T> collection) : base(collection)
        {
        }

        public NeutronQueue(System.Int32 capacity) : base(capacity)
        {
        }
    }
}