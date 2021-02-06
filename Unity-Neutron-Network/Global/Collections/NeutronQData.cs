using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronQueueData : ConcurrentQueue<byte[]>
    {
        public delegate Task OnChanged();
        public event OnChanged onChanged;
        public new void Enqueue(byte[] data)
        {
            base.Enqueue(data); // thread-safe
            onChanged?.Invoke(); // Thread safe, delegates are immutable.
        }
    }
}