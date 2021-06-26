using System.Threading;

namespace NeutronNetwork.Internal
{
    public class NeutronStatisticsProfiler
    {
        #region Fields
        private int bytesOutgoing;
        private int bytesIncoming;
        #endregion

        public void AddIncoming(int value)
        {
            Interlocked.Add(ref bytesIncoming, value);
        }

        public void AddOutgoing(int value)
        {
            Interlocked.Add(ref bytesOutgoing, value);
        }

        public bool Get(out int Outgoing, out int Incoming)
        {
            Outgoing = Interlocked.CompareExchange(ref bytesOutgoing, 0, 0);
            Incoming = Interlocked.CompareExchange(ref bytesIncoming, 0, 0);
            return true;
        }

        public void Reset()
        {
            Interlocked.Exchange(ref bytesOutgoing, 0);
            Interlocked.Exchange(ref bytesIncoming, 0);
        }
    }
}