using System;
using UnityEngine;

namespace NeutronNetwork.Server.Internal
{
    [Serializable]
    public class Handle
    {
        public SendTo sendTo;
        public Broadcast broadcast;
        public Protocol protocol;
        public Handle(SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            this.sendTo = sendTo;
            this.broadcast = broadcast;
            this.protocol = protocol;
        }
    }
}