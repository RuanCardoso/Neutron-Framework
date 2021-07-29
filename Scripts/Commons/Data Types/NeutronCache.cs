using NeutronNetwork;
using System;

[Serializable]
public class NeutronCache
{
    #region Fields
    private int _id;
    private byte[] _buffer;
    private NeutronPlayer _owner;
    private CachedPacket _packet;
    private Cache _cache;
    #endregion

    #region Properties
    public int Id { get => _id; set => _id = value; }
    public byte[] Buffer { get => _buffer; set => _buffer = value; }
    public NeutronPlayer Owner { get => _owner; set => _owner = value; }
    public CachedPacket Packet { get => _packet; set => _packet = value; }
    public Cache Cache { get => _cache; set => _cache = value; }
    #endregion

    public NeutronCache(int id, byte[] buffer, NeutronPlayer owner, CachedPacket packet, Cache cache)
    {
        _id = id;
        _buffer = buffer;
        _owner = owner;
        _packet = packet;
        _cache = cache;
    }
}