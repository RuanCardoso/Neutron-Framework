using System;
using System.Net.Sockets;

namespace NeutronNetwork
{
    [AttributeUsage(AttributeTargets.Field)]
    public class SyncAttribute : Attribute
    {
        public string onChanged;
        public bool serverOnly;
        public SendTo sendTo;
        public Broadcast broadcast;
        public Protocol protocolType;

        public SyncAttribute(Broadcast broadcast, bool serverOnly = true, SendTo sendTo = SendTo.All, Protocol protocolType = Protocol.Tcp, string onChanged = null)
        {
            this.onChanged = onChanged;
            this.serverOnly = serverOnly;
            this.broadcast = broadcast;
            this.protocolType = protocolType;
            this.sendTo = sendTo;
        }
    }
}