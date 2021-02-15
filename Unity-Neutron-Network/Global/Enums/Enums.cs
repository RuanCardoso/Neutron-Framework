using System;

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
    RPC,
    APC,
    Static,
    Response,
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
    Disconnected,
    SyncBehaviour,
    Nickname,
    SetPlayerProperties,
    SetRoomProperties,
    Test,
    //======================================================
    // - CUSTOM PACKETS ADD HERE
    //======================================================
}

public enum CachedPacket : byte
{
    /// <summary>
    /// Used to instantiate other players on this client.
    /// </summary>
    Static = 121,
    RPC = 122,
    APC = 123,
    Response = 124,
    //======================================================
    // - CUSTOM PACKETS ADD HERE
    //======================================================
}

public enum Compression : byte
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
    None,
    /// <summary>
    /// Broadcast data on the server.
    /// </summary>
    All,
    /// <summary>
    /// Broadcast data on the channel.
    /// </summary>
    Channel,
    /// <summary>
    /// Broadcast data on the room.
    /// </summary>
    Room,
    /// <summary>
    /// Broadcast data on the room, except for those in the lobby/waiting room.
    /// that is only for players who are instantiated and in the same room/channel.
    /// </summary>
    Instantiated,
    /// <summary>
    /// Broadcast data on the same group.
    /// </summary>
    Group
}

public enum ClientType : byte
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