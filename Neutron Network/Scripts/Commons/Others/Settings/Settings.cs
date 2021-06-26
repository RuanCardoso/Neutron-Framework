using System;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Server.Internal;
using UnityEngine;

namespace NeutronNetwork.Constants
{
    [Serializable]
    public class NeutronGlobalSettings
    {
        public string Address = "127.0.0.1";
        public int Port = 5055;
        public int MaxPlayers = 1000;
        public Serialization Serialization = Serialization.Json;
        public Compression Compression = Compression.Deflate;
        #region NotImplemented
        [ReadOnly] public bool Lan;
        #endregion
        public bool NoDelay = true;
    }

    [Serializable]
    public class NeutronEditorSettings
    {
        public int FPS = 60;
        public int DispatcherChunkSize = 1;
    }

    [Serializable]
    public class NeutronServerSettings
    {
        public int FPS = 60;
        public int BackLog = 10;
        public int DispatcherChunkSize = 1;
        public int PacketChunkSize = 1;
        public int ProcessChunkSize = 1;
        [HideInInspector] public bool NeutronAntiCheat = true;
    }

    [Serializable]
    public class NeutronClientSettings
    {
        public int FPS = 60;
        public int DispatcherChunkSize = 1;
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
}