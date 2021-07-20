using System.Threading;

namespace NeutronNetwork.Internal
{
    public class NSP
    {
        #region Fields
        private int _bytesOutgoing;
        private int _bytesIncoming;
        #endregion

        public void AddIncoming(int value)
        {
            Interlocked.Add(ref _bytesIncoming, value);
        }

        public void AddOutgoing(int value)
        {
            Interlocked.Add(ref _bytesOutgoing, value);
        }

        public void Get(out int Outgoing, out int Incoming)
        {
            Outgoing = Interlocked.CompareExchange(ref _bytesOutgoing, 0, 0);
            Incoming = Interlocked.CompareExchange(ref _bytesIncoming, 0, 0);
        }

        public void Reset()
        {
            Interlocked.Exchange(ref _bytesOutgoing, 0);
            Interlocked.Exchange(ref _bytesIncoming, 0);
        }
    }
}