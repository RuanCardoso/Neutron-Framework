using System.Collections.Concurrent;

namespace NeutronNetwork.Internal.Wrappers
{
    public class NeutronQueueData : ConcurrentQueue<byte[]>
    {
        public delegate void OnChanged();
        public event OnChanged onChanged;
        public new void Enqueue(byte[] data)
        {
            base.Enqueue(data);
            onChanged?.Invoke();
        }
    }
}