using NeutronNetwork.Server.Internal;

namespace NeutronNetwork.Server
{
    public class ServerPackets : ServerBase
    {
        public void iRPC(int nID, int DynamicID, NeutronWriter options, NeutronPlayer owner, Cache cacheMode, TargetTo sendTo, TunnelingTo broadcast, Protocol nRecProtocol)
        {
            NeutronPlayer Sender = owner;
            //NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            iRPCHandler(Sender, broadcast, sendTo, cacheMode, nID, DynamicID, options.ToArray(), nRecProtocol);
        }

        public void gRPC(NeutronPlayer sender, int nonDynamicID, NeutronWriter options)
        {
            gRPCHandler(sender, sender.ID, nonDynamicID, options.ToArray());
        }

        public void OnSerializeView(NeutronPlayer sender, NeutronView nView, int nID, NeutronWriter options, TargetTo sendTo, TunnelingTo broadcast, Protocol nSendProtocol)
        {
            //OnSerializeViewHandler(sender, nView.ID, nID, options.ToArray(), sendTo, broadcast, nRecProtocol);
            using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.WritePacket(Packet.OnAutoSync);
                nWriter.WritePacket(nSendProtocol);
                nWriter.WritePacket(sendTo);
                nWriter.WritePacket(broadcast);
                nWriter.Write(nView.ID);
                nWriter.Write(nID);
                nWriter.Write(options);
                Neutron.Server.m_DataForProcessing.Add(new NeutronData(nWriter.ToArray(), sender, Protocol.Tcp));
            }
        }
    }
}