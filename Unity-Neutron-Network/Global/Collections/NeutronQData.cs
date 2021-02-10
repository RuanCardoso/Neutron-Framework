using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronQueueData : ConcurrentQueue<DataBuffer>
    {
        /// <summary>
        /// signal to process data.
        /// </summary>
        public ManualResetEvent manualResetEvent = new ManualResetEvent(false);
        public new void Enqueue(DataBuffer data)
        {
            base.Enqueue(data); // thread-safe
            ///////////////////////////////////////
            manualResetEvent.Set(); // signal to process data.
        }
    }
}