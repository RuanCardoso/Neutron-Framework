using System.Threading;

namespace NeutronNetwork.Editor
{
    public class InOutData
    {
        private int _bytesOutgoing;
        private int _bytesIncoming;
        private int _packetsOutgoing;
        private int _packetsIncoming;

        public void AddIncoming(int value)
        {
            Interlocked.Add(ref _bytesIncoming, value);
            Interlocked.Add(ref _packetsIncoming, 1);
        }

        public void AddOutgoing(int value)
        {
            Interlocked.Add(ref _bytesOutgoing, value);
            Interlocked.Add(ref _packetsOutgoing, 1);
        }

        public void Get(out int bytesOutgoing, out int bytesIncoming, out int packetsOutgoing, out int packetsIncoming)
        {
            bytesOutgoing = Interlocked.CompareExchange(ref _bytesOutgoing, 0, 0);
            bytesIncoming = Interlocked.CompareExchange(ref _bytesIncoming, 0, 0);
            packetsOutgoing = Interlocked.CompareExchange(ref _packetsOutgoing, 0, 0);
            packetsIncoming = Interlocked.CompareExchange(ref _packetsIncoming, 0, 0);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _bytesOutgoing, 0);
            Interlocked.Exchange(ref _bytesIncoming, 0);
            Interlocked.Exchange(ref _packetsOutgoing, 0);
            Interlocked.Exchange(ref _packetsIncoming, 0);
        }
    }
}