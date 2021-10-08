using NeutronNetwork.Wrappers;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronSafeQueueNonAlloc<T>
    {
        private readonly NeutronQueue<T> _queue = new NeutronQueue<T>();
        //* Objeto usado para sincronizar a inserção e remoção de objetos em multiplos threads.
        private readonly object _lock = new object();
        //* Define a capacidade máxima do queue.
        private readonly int _capacity = 0;
        public NeutronSafeQueueNonAlloc(int capacity)
        {
            _capacity = capacity;
        }

        public void Enqueue(T item)
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

        public T Dequeue()
        {
            lock (_lock)
            {
                T item = _queue.Dequeue();
                return item;
            }
        }

        public bool TryDequeue(out T item)
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

        public int Count {
            get {
                lock (_lock)
                {
                    int count = _queue.Count;
                    return count;
                }
            }
        }
    }
}