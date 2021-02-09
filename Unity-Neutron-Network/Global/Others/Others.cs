using NeutronNetwork;
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