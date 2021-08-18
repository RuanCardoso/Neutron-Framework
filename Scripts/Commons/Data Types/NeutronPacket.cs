using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using System;

namespace NeutronNetwork.Internal
{
    [Serializable]
    public class NeutronPacket
    {
        public byte[] Buffer { get; set; }
        public NeutronPlayer Owner { get; set; }
        public NeutronPlayer Sender { get; set; }
        public Protocol Protocol { get; set; }
        public Packet Packet { get; set; } = Packet.Empty;
        public bool IsServerSide { get; set; }

        public NeutronPacket()
        { }

        public NeutronPacket(byte[] buffer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
        {
            Buffer = buffer;
            Owner = owner;
            Sender = sender;
            Protocol = protocol;
        }

        public NeutronPacket(NeutronWriter writer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
        {
            Buffer = writer.ToArray();
            Owner = owner;
            Sender = sender;
            Protocol = protocol;
        }

        public NeutronPacket(INeutronWriter writer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol)
        {
            Buffer = writer.ToArray();
            Owner = owner;
            Sender = sender;
            Protocol = protocol;
        }

        public NeutronPacket(byte[] buffer, NeutronPlayer owner, NeutronPlayer sender, Protocol protocol, Packet packet)
        {
            Buffer = buffer;
            Owner = owner;
            Sender = sender;
            Protocol = protocol;
            Packet = packet;
        }

        public void Recycle()
        {
            Neutron.PooledNetworkPackets.Push(this);
        }
    }
}