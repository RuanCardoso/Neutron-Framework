using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Wrappers;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeQueueNonAlloc<T> : INeutronConsumer<T>
    {
        private readonly NeutronQueue<T> _queue = new NeutronQueue<T>();
        //* Objeto usado para sincronizar a inserção e remoção de objetos em multiplos threads.
        private readonly object _lock = new object();
        //* Define a capacidade máxima do queue.
        private readonly int _capacity = 0;

        public NeutronSafeQueueNonAlloc() { }

        public NeutronSafeQueueNonAlloc(int capacity)
        {
            _capacity = capacity;
        }

        public void Push(T item)
        {
            lock (_lock)
            {
                if (_capacity > 0)
                {
                    if (_queue.Count < _capacity)
                        _queue.Enqueue(item);
                }
                else
                    _queue.Enqueue(item);
            }
        }

        public T Pull()
        {
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    T item = _queue.Dequeue();
                    return item;
                }
                else
                    return default;
            }
        }

        public bool TryPull(out T item)
        {
            item = default;
            lock (_lock)
            {
                if (_queue.Count > 0)
                {
                    item = _queue.Dequeue();
                    return true;
                }
                else
                    return false;
            }
        }

        public int Count
        {
            get
            {
                lock (_lock)
                {
                    int count = _queue.Count;
                    return count;
                }
            }
        }

        public NeutronQueue<T> Queue => _queue;

        public void Dispose()
        { }
    }
}