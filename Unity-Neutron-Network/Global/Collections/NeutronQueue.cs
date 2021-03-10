using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronQueue<T> : Queue<T>
    {
        //* object used to synchronize a list.
        private readonly object syncRoot = new object();
        /// <summary>
        ///* signal to process data.
        /// </summary>
        public ManualResetEvent mEvent = new ManualResetEvent(false);
        public new void Enqueue(T item)
        {
            base.Enqueue(item);
            mEvent.Set(); //* Sets the state of the event to signaled, which allows one or more waiting threads to proceed.
        }

        public void SafeEnqueue(T item)
        {
            lock (syncRoot)
            {
                base.Enqueue(item);
                mEvent.Set(); //* Sets the state of the event to signaled, which allows one or more waiting threads to proceed.
            }
        }

        public new T Dequeue() => base.Dequeue();

        public T SafeDequeue() // thread-safe
        {
            lock (syncRoot)
            {
                return base.Dequeue();
            }
        }

        public new int Count { get => base.Count; }

        public int SafeCount
        {
            get
            {
                lock (syncRoot)
                {
                    return base.Count;
                }
            }
        }
    }
}