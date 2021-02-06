using NeutronNetwork.Internal.Extesions;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Extesions
{
    public static class Extesions
    {
        public static void APC(this NeutronView statePlayer, int APCID, NeutronWriter parameters, SendTo sendTo, Broadcast broadcast, Protocol type = Protocol.Tcp)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] param = parameters.ToArray();

                writer.WritePacket(Packet.APC);
                writer.Write(APCID);
                writer.Write(statePlayer.owner.ID);
                writer.Write(param.Length);
                writer.Write(param);
                statePlayer.owner.Send(sendTo, writer.ToArray(), broadcast, type);
            }
        }

        public static void Response(this NeutronView statePlayer, MonoBehaviour mThis, int ResponseID, NeutronWriter parameters, SendTo sendTo, Protocol protocolType, Broadcast broadcast)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] param = parameters.ToArray();

                writer.WritePacket(Packet.Response);
                writer.Write(ResponseID);
                writer.Write(param.Length);
                writer.Write(param);
                statePlayer.owner.Send(sendTo, writer.ToArray(), broadcast, protocolType);
            }
        }
    }
}