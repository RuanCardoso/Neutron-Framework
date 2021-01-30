using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using System;

[Serializable]
public class CachedBuffer
{
    [ReadOnly] public int ID;
    [NonSerialized] public byte[] buffer;
    public Player owner;
    [ReadOnly] public CachedPacket cachedPacket;
}