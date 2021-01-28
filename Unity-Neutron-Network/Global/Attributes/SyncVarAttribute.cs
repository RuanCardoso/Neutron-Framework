using System;
using System.Net.Sockets;

[AttributeUsage(AttributeTargets.Field)]
public class SyncVarAttribute : Attribute
{
    public string function;
    public bool serverOnly;
    public SendTo sendTo;
    public Broadcast broadcast;
    public ProtocolType protocolType;

    public SyncVarAttribute(Broadcast broadcast, bool serverOnly = true, SendTo sendTo = SendTo.All, ProtocolType protocolType = ProtocolType.Tcp, string function = null)
    {
        this.function = function;
        this.serverOnly = serverOnly;
        this.broadcast = broadcast;
        this.protocolType = protocolType;
        this.sendTo = sendTo;
    }
}