using System.Collections;
using System.Collections.Generic;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using UnityEngine;
namespace NeutronNetwork.Internal.Server
{
    public class NeutronServerPublicFunctions : NeutronServerFunctions
    {
        public void Dynamic(int nID, int DynamicID, NeutronWriter options, Player owner, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            Player Sender = owner;
            NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            HandleDynamic(Sender, broadcast, sendTo, cacheMode, nID, DynamicID, options.ToArray(), infor.Serialize(), protocol);
        }

        public void NonDynamic(Player sender, int nonDynamicID, NeutronWriter options, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            HandleNonDynamic(sender, broadcast, sendTo, cacheMode, nonDynamicID, options.ToArray(), protocol);
        }
    }
}