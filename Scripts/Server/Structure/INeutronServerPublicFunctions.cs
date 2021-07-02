using System.Collections;
using System.Collections.Generic;
using NeutronNetwork.Internal.Server;
using UnityEngine;

namespace NeutronNetwork.Server
{
    public class NeutronServerPublicFunctions : NeutronServerFunctions
    {
        public void iRPC(int nID, int DynamicID, NeutronWriter options, Player owner, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            Player Sender = owner;
            //NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            DynamicHandler(Sender, broadcast, sendTo, cacheMode, nID, DynamicID, options.ToArray(), protocol);
        }

        public void gRPC(Player sender, int nonDynamicID, NeutronWriter options)
        {
            sRPCHandler(sender, sender.ID, nonDynamicID, options.ToArray());
        }
    }
}