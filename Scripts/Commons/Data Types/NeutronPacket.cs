using NeutronNetwork.Packets;
using System;

namespace NeutronNetwork.Internal
{
    [Serializable]
    public class NeutronPacket
    {
        public byte[] Buffer {
            get;
            set;
        }

        public NeutronPlayer Owner {
            get;
            set;
        }

        public NeutronPlayer Sender {
            get;
            set;
        }

        public Protocol Protocol {
            get;
            set;
        }

        public bool IsServerSide {
            get;
            set;
        }

        public NeutronPacket()
        { }

        public NeutronPacket(byte[] buffer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
        {
            Buffer = buffer;
            Owner = owner;
            Sender = sender;
            Protocol = protocol;
        }

        public void Recycle()
        {
            //* Reseta antes de reciclar, se não resetar, quando o cliente enviar o pacote, vai ser true.... se passando pelo servidor, quando na verdade é o cliente....
            IsServerSide = false;
            //--------------------------------------
            Neutron.PooledNetworkPackets.Push(this);
        }
    }
}