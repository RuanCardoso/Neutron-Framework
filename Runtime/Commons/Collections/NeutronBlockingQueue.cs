using NeutronNetwork.Internal.Interfaces;
using System.Collections.Concurrent;

namespace NeutronNetwork.Wrappers
{
    public class NeutronBlockingQueue<T> : BlockingCollection<T>, INeutronConsumer<T>
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

        public T Pull()
        {
            return base.Take();
        }

        public void Push(T item)
        {
            base.Add(item);
        }

        public bool TryPull(out T item)
        {
            return base.TryTake(out item);
        }

        public new void Dispose()
        {
            base.Dispose();
        }
    }
}