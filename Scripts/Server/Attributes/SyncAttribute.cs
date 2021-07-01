using System;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork
{
    [AttributeUsage(AttributeTargets.Field, AllowMultiple = false, Inherited = true)]
    public class SyncAttribute : PropertyAttribute
    {
        public SendTo sendTo = SendTo.All;
        public Broadcast broadcast = Broadcast.Auto;
        public Protocol protocol = Protocol.Tcp;

        public SyncAttribute() { }

        public SyncAttribute(SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            this.sendTo = sendTo;
            this.broadcast = broadcast;
            this.protocol = protocol;
        }
    }
}