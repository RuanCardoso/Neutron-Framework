using NeutronNetwork.Internal.Packets;

namespace NeutronNetwork.Extensions
{
    public static class PlayerExt
    {
        /// <summary>
        ///* Envia uma mensagem para o jogador.<br/>
        ///* (Server-Side).
        /// </summary>
        /// <param name="player"></param>
        /// <param name="packet"></param>
        /// <param name="message"></param>
        public static void Error(this NeutronPlayer player, Packet packet, string message, int errorCode = 0)
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                //*********************************************
                writer.WritePacket((byte)Packet.Error);
                writer.WritePacket((byte)packet);
                writer.Write(message);
                writer.Write(errorCode);
                //*********************************************
                player.Write(writer);
            }
        }
    }
}