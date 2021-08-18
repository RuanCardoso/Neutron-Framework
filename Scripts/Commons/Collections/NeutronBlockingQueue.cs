using System.Collections.Concurrent;

namespace NeutronNetwork.Wrappers
{
    public class NeutronBlockingQueue<T> : BlockingCollection<T>
    {
        public NeutronBlockingQueue()
        {
        }

        public NeutronBlockingQueue(IProducerConsumerCollection<T> collection) : base(collection)
        {
        }

        public NeutronBlockingQueue(int boundedCapacity) : base(boundedCapacity)
        {
        }

        public NeutronBlockingQueue(IProducerConsumerCollection<T> collection, int boundedCapacity) : base(collection, boundedCapacity)
        {
        }
    }
}