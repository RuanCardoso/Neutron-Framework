﻿using System;

public enum SendTo : byte
{
    /// <summary>
    /// Broadcast data to all Players, including you.
    /// </summary>
    All,
    /// <summary>
    /// Broadcast data only you.
    /// </summary>
    Only,
    /// <summary>
    /// Broadcast data to all Players, except you.
    /// </summary>
    Others,
}

public enum Packet : byte
{
    Connected,
    DisconnectedByReason,
    Login,
    Dynamic,
    NonDynamic,
    JoinChannel,
    JoinRoom,
    LeaveRoom,
    LeaveChannel,
    CreateRoom,
    SendChat,
    GetChannels,
    GetChached,
    GetRooms,
    Fail,
    DestroyPlayer,
    VoiceChat,
    PlayerDisconnected,
    Nickname,
    SetPlayerProperties,
    SetRoomProperties,
    Heartbeat,
    Test,
    //======================================================
    // - CUSTOM PACKETS ADD HERE
    //======================================================
}

public enum CachedPacket : byte
{
    NonDynamic = 121,
    Dynamic = 122,
    //======================================================
    // - CUSTOM PACKETS ADD HERE
    //======================================================
}

public enum Compression : int
{
    /// <summary>
    /// Disable data compression.
    /// </summary>
    None,
    /// <summary>
    /// Compress data using deflate mode.
    /// </summary>
    Deflate,
    /// <summary>
    /// Compress data using GZip mode.
    /// </summary>
    Gzip,
}

public enum Broadcast : byte
{
    /// <summary>
    /// None broadcast. Used to SendTo.Only.
    /// </summary>
    Only,
    /// <summary>
    /// Broadcast data on the server.
    /// </summary>
    Server,
    /// <summary>
    /// Broadcast data on the channel.
    /// </summary>
    Channel,
    /// <summary>
    /// Broadcast data on the room.
    /// </summary>
    Room,
    /// <summary>
    /// Broadcast data on the same group.
    /// </summary>
    Group,
    /// <summary>
    /// Broadcast data on the same room or channel.
    /// </summary>
    Auto,
}

public enum ClientType : int
{
    MainPlayer,
    Bot,
    VirtualPlayer,
}

public enum Protocol : byte
{
    Tcp = 6,
    Udp = 17,
}

public enum Serialization : int
{
    BinaryFormatter,
    Json,
}

public enum Statistics : int
{
    ClientSent,
    ClientRec,
    ServerSent,
    ServerRec
}

public enum AuthorityMode : int
{
    Server,
    Owner,
    OwnerAndServer,
    MasterClient,
    IgnoreExceptServer,
    Ignore,
}

public enum CacheMode : byte
{
    None, Overwrite, Append
}

[Flags]
public enum ComponentMode : int
{
    IsMine = 2, IsServer = 4
}
public enum SmoothMode : int { Lerp, MoveTowards }
public enum ParameterMode : int { Sync, NonSync }
public enum Ambient : int { Server, Client, Both }