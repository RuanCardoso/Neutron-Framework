using System;
using System.Net;
using System.Net.Sockets;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Internal
{
    //* Uma gambiarra para evitar alocaçoes em excesso pelo socket Udp, coé microsoft resolve isso ae pá noix.
    public class NonAllocEndPoint : IPEndPoint
    {
        public SocketAddress SocketAddress
        {
            get;
            set;
        }

        public IPEndPoint IPEndPoint
        {
            get;
            set;
        }

        public NonAllocEndPoint(long address, int port) : base(address, port)
        {
            SocketAddress = base.Serialize();
        }

        public NonAllocEndPoint(IPAddress address, int port) : base(address, port)
        {
            SocketAddress = base.Serialize();
        }

        public override SocketAddress Serialize()
        {
            return SocketAddress;
        }

        public override EndPoint Create(SocketAddress socketAddress)
        {
            if (socketAddress.Family != AddressFamily)
                throw new Exception($"0x0000001 fatal error ):");
            if (socketAddress.Size < 8)
                throw new Exception($"0x0000002 fatal error ):");

            if (SocketAddress != socketAddress)
            {
                SocketAddress = socketAddress;

                unchecked
                {
                    SocketAddress[0] += 1;
                    SocketAddress[0] -= 1;
                }

                if (SocketAddress.GetHashCode() == 0)
                    throw new Exception($"0x0000003 fatal error ):");
            }

            //if (IPEndPoint == null)
            //    IPEndPoint = GetIPEndPoint();

            return this;
        }

        //private byte GetBuffer(int offset)
        //{
        //    return SocketAddress[offset];
        //}

        //private void SetBuffer(int offset, int value)
        //{
        //    SocketAddress[offset] = (byte)value;
        //}

        //private IPEndPoint GetIPEndPoint()
        //{
        //    IPAddress address = GetIPAddress();
        //    int port = (int)((GetBuffer(2) << 8 & 0xFF00) | (GetBuffer(3)));
        //    return new IPEndPoint(address, port);
        //}

        //private IPAddress GetIPAddress()
        //{
        //    if (AddressFamily == AddressFamily.InterNetworkV6)
        //    {
        //        byte[] address = new byte[16];
        //        for (int i = 0; i < address.Length; i++)
        //        {
        //            address[i] = GetBuffer(i + 8);
        //        }

        //        long scope = (long)((GetBuffer(27) << 24) +
        //                            (GetBuffer(26) << 16) +
        //                            (GetBuffer(25) << 8) +
        //                            (GetBuffer(24)));

        //        return new IPAddress(address, scope);

        //    }
        //    else if (AddressFamily == AddressFamily.InterNetwork)
        //    {
        //        long address = (long)(
        //                (GetBuffer(4) & 0x000000FF) |
        //                (GetBuffer(5) << 8 & 0x0000FF00) |
        //                (GetBuffer(6) << 16 & 0x00FF0000) |
        //                (GetBuffer(7) << 24)
        //                ) & 0x00000000FFFFFFFF;

        //        return new IPAddress(address);

        //    }
        //    else
        //        throw new SocketException((int)SocketError.AddressFamilyNotSupported);
        //}

        public override int GetHashCode() => SocketAddress.GetHashCode();
    }
}