﻿using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using System;

[Serializable]
public class CachedBuffer
{
    [ReadOnly] public int ID;
    [NonSerialized] public byte[] buffer;
    [NonSerialized] public Player owner;
    [ReadOnly] public CachedPacket cachedPacket;
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