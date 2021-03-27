using System.Collections;
using System.Collections.Generic;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server;
using UnityEngine;
namespace NeutronNetwork.Internal.Server
{
    public class NeutronServerPublicFunctions : NeutronServerFunctions
    {
        public void Dynamic(int nID, int DynamicID, NeutronWriter options, Player owner, SendTo sendTo, bool isCached, Broadcast broadcast, Protocol protocol)
        {
            Player Sender = owner;
            NeutronMessageInfo infor = new NeutronMessageInfo(CurrentTime);
            HandleDynamic(Sender, broadcast, sendTo, nID, DynamicID, isCached, options.ToArray(), infor.Serialize(), protocol);
        }

        public void NonDynamic(Player sender, int nonDynamicID, bool isCached, NeutronWriter options, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            HandleNonDynamic(sender, broadcast, sendTo, nonDynamicID, isCached, options.ToArray(), protocol);
        }
    }
}