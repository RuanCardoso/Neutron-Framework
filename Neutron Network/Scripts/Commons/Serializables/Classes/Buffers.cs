using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using System;

[Serializable]
public class CachedBuffer
{
    public int attributeID;
    public byte[] buffer;
    public Player owner;
    public CachedPacket cachedPacket;
    public CacheMode cacheMode;
    public CachedBuffer(int attributeID, byte[] buffer, Player owner, CachedPacket cachedPacket, CacheMode cacheMode)
    {
        this.attributeID = attributeID;
        this.buffer = buffer;
        this.owner = owner;
        this.cachedPacket = cachedPacket;
        this.cacheMode = cacheMode;
    }
}

[Serializable]
public class DataBuffer
{
    public byte[] buffer;
    public Player player;
    public Protocol protocol;

    public DataBuffer(byte[] buffer, Player player, Protocol protocol)
    {
        this.buffer = buffer;
        this.player = player;
        this.protocol = protocol;
    }

    public DataBuffer(byte[] buffer, Protocol protocol)
    {
        this.buffer = buffer;
        this.protocol = protocol;
    }
}