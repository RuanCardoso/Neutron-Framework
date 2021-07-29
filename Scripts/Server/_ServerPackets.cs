/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Server
{
    public class ServerPackets : ServerBase
    {
#pragma warning disable IDE1006
        public void iRPC(int viewId, int id, NeutronWriter writer, NeutronPlayer player, Cache cache, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
#pragma warning restore IDE1006
        {
            NeutronPlayer Sender = player;
            //NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            iRPCHandler(Sender, tunnelingTo, targetTo, cache, viewId, id, writer.ToArray(), protocol);
        }

#pragma warning disable IDE1006
        public void gRPC(NeutronPlayer player, int id, NeutronWriter writer)
#pragma warning restore IDE1006
        {
            gRPCHandler(player, player.ID, id, writer.ToArray());
        }

        public void OnAutoSynchronization(NeutronPlayer player, NeutronView view, int instanceId, NeutronWriter writer, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol protocol)
        {
            //OnSerializeViewHandler(sender, nView.ID, nID, options.ToArray(), sendTo, broadcast, nRecProtocol);
            using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
            {
                nWriter.WritePacket(Packet.OnAutoSync);
                nWriter.WritePacket(protocol);
                nWriter.WritePacket(view.RegisterType);
                nWriter.WritePacket(targetTo);
                nWriter.WritePacket(tunnelingTo);
                nWriter.Write(view.ID);
                nWriter.Write(instanceId);
                nWriter.Write(writer);
                Neutron.Server.OnSimulatingReceivingData(new NeutronData(nWriter.ToArray(), player, Protocol.Tcp));
            }
        }
    }
}