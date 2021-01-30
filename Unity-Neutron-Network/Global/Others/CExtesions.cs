using NeutronNetwork.Internal.Extesions;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Extesions
{
    public static class Extesions
    {
        public static void APC(this ServerView statePlayer, int APCID, NeutronWriter parameters, SendTo sendTo, Broadcast broadcast, ProtocolType type = ProtocolType.Tcp)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.APC);
                writer.Write(APCID);
                writer.Write(statePlayer.player.ID);
                writer.Write(parameters.ToArray());
                statePlayer.player.Send(sendTo, writer.ToArray(), broadcast, null, type);
            }
        }

        public static void ACC(this ServerView statePlayer, MonoBehaviour mThis, int ACCID, NeutronWriter parameters, SendTo sendTo, ProtocolType protocolType, Broadcast broadcast)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.ACC);
                writer.Write(ACCID);
                writer.Write(mThis.GetType().Name);
                writer.Write(parameters.ToArray());
                statePlayer.player.Send(sendTo, writer.ToArray(), broadcast, null, protocolType);
            }
        }
    }
}