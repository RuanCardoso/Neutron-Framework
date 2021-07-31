using NeutronNetwork;
using System;

[Serializable]
public class NeutronData
{
    #region Fields
    private byte[] _buffer;
    private NeutronPlayer _player;
    private Protocol _protocol;
    private Packet _packet;
    #endregion

    #region Properties
    public byte[] Buffer { get => _buffer; set => _buffer = value; }
    public NeutronPlayer Player { get => _player; set => _player = value; }
    public Protocol Protocol { get => _protocol; set => _protocol = value; }
    public Packet Packet { get => _packet; set => _packet = value; }
    #endregion

    public NeutronData(byte[] buffer, Protocol protocol)
    {
        _buffer = buffer;
        _protocol = protocol;
    }

    public NeutronData(byte[] buffer, Protocol protocol, Packet packet)
    {
        _buffer = buffer;
        _protocol = protocol;
        _packet = packet;
    }

    public NeutronData(byte[] buffer, NeutronPlayer player, Protocol protocol)
    {
        _buffer = buffer;
        _player = player;
        _protocol = protocol;
    }

    public NeutronData(byte[] buffer, NeutronPlayer player, Protocol protocol, Packet packet)
    {
        _buffer = buffer;
        _player = player;
        _protocol = protocol;
        _packet = packet;
    }
}