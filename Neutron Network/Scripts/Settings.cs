using System;
using UnityEngine;

[Serializable]
public class NeutronGlobalSettings
{
    public string Address = "127.0.0.1";
    public int Port = 5055;
    public Serialization Serialization = Serialization.Json;
    public Compression Compression = Compression.Deflate;
    public bool NoDelay = true;
}

[Serializable]
public class NeutronServerSettings
{
    public int BackLog = 10;
    [Range(1, 3600)] public int FPS = 60;
    [Range(1, 500)] public int MonoChunkSize = 1;
    [Range(1, 500)] public int PacketChunkSize = 1;
    [Range(1, 500)] public int ProcessChunkSize = 1;
    public bool AntiCheat = true;
}

[Serializable]
public class NeutronClientSettings
{
    [Range(1, 120)] public int FPS = 60;
    [Range(1, 500)] public int MonoChunkSize = 1;
}

[Serializable]
public class NeutronPermissionsSettings
{

}

[Serializable]
public class NeutronHandleSettings
{
    public Handle OnPlayerNicknameChanged = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerDisconnected = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerJoinedChannel = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerJoinedRoom = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerLeaveRoom = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerLeaveChannel = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerPropertiesChanged = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnRoomPropertiesChanged = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
    public Handle OnPlayerDestroyed = new Handle(SendTo.All, Broadcast.Auto, Protocol.Tcp);
}