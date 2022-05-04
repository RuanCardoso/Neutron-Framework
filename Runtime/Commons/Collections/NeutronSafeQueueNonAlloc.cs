using System.Linq;
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
        private readonly bool _sort = false;

        public NeutronSafeQueueNonAlloc(bool sort = false) { _sort = sort; }

        public NeutronSafeQueueNonAlloc(int capacity, bool sort = false)
        {
            _capacity = capacity;
            _sort = sort;
        }

        public void Push(T item, bool sort = false)
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

                if (sort)
                    Sort();
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

        public void Sort()
        {
            if (_sort)
            {
                var values = _queue.ToList();
                values.Sort();
                _queue.Clear();
                foreach (var value in values)
                    _queue.Enqueue(value);
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