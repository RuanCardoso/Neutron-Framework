using System;
using System.Collections.Generic;
using System.IO;
using System.Net;
using System.Net.Sockets;
using UnityEngine;

public class SocketConfig
{
    public const int BUFFER_SIZE = 28;
    public byte[] buffer = new byte[BUFFER_SIZE];
}

public class UDPBuffer : SocketConfig
{ }

public class TCPBuffer : SocketConfig
{ }

public class MessageFraming
{
    public NeutronWriter memoryBuffer = new NeutronWriter();
    public int lengthOfPacket = -1;
    public int offset;
}

[Serializable]
public class CachedBuffer
{
    [ReadOnly] public int ID;
    [NonSerialized] public byte[] buffer;
    public Player owner;
    [ReadOnly] public CachedPacket cachedPacket;
}