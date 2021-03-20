using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using System;

[Serializable]
public class CachedBuffer
{
    public int ID;
    public byte[] buffer;
    public Player owner;
    public CachedPacket cachedPacket;
}

[Serializable]
public class DataBuffer
{
    public Protocol protocol;
    public byte[] buffer;
    public Player player;

    public DataBuffer(Protocol protocol, Byte[] buffer)
    {
        this.protocol = protocol;
        this.buffer = buffer;
    }

    public DataBuffer(Protocol protocol, Byte[] buffer, Player player)
    {
        this.protocol = protocol;
        this.buffer = buffer;
        this.player = player;
    }
}