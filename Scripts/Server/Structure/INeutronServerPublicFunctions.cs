using System.Collections;
using System.Collections.Generic;
using NeutronNetwork.Internal.Server;
using UnityEngine;

namespace NeutronNetwork.Server
{
    public class NeutronServerPublicFunctions : NeutronServerFunctions
    {
        public void iRPC(int nID, int DynamicID, NeutronWriter options, Player owner, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol nRecProtocol, Protocol protocol)
        {
            Player Sender = owner;
            //NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            iRPCHandler(Sender, broadcast, sendTo, cacheMode, nID, DynamicID, options.ToArray(), nRecProtocol, protocol);
        }

        public void gRPC(Player sender, int nonDynamicID, NeutronWriter options)
        {
            gRPCHandler(sender, sender.ID, nonDynamicID, options.ToArray());
        }

        public void OnSerializeView(Player sender, NeutronView nView, int nID, NeutronWriter options, SendTo sendTo, Broadcast broadcast, Protocol nRecProtocol)
        {
            OnSerializeViewHandler(sender, nView.ID, nID, options.ToArray(), sendTo, broadcast, nRecProtocol);
        }
    }
}