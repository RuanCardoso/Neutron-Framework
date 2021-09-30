using System.Net;
using System.Threading.Tasks;

namespace NeutronNetwork.Internal
{
    public class StateObject
    {
        public static int Size {
            get;
            set;
        }

        public byte[] Buffer {
            get;
            set;
        } = new byte[Size];

        public byte[] ReceivedDatagram {
            get;
            set;
        }

        public byte[] SendDatagram {
            get;
            set;
        }

        public IPEndPoint TcpRemoteEndPoint {
            get;
            set;
        }

        public IPEndPoint UdpLocalEndPoint {
            get;
            set;
        }

        public IPEndPoint UdpRemoteEndPoint {
            get;
            set;
        }
        //* Essa porra aqui é uma gambiarra pra evitar alocações.
        //* Levei tipo.... horas, pra conseguir fazer isso ):
        public EndPoint NonAllocEndPoint = new NonAllocEndPoint(IPAddress.Any, 0);

        public bool UdpIsReady()
        {
            return UdpRemoteEndPoint != null;
        }
    }
}