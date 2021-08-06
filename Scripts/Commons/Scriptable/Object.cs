using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Server.Internal;
using System;
using UnityEngine;

namespace NeutronNetwork.Constants
{
    [Serializable]
    public class NeutronGlobalSettings
    {
        public string[] Addresses = { "localhost" };
        public int Port = 1418;
        [ReadOnly] [AllowNesting] public string AppId;
        [Range(1, Int16.MaxValue)] public int MaxPlayers = 300;
        [Range(1, Int16.MaxValue)] public int PoolCapacity = 500;
        public Serialization Serialization = Serialization.Json;
        public Compression Compression = Compression.None;
        public bool NoDelay = true;
        [ReadOnly] [AllowNesting] public bool PeerToPeer;
        [HideInInspector] public bool PerfomanceMode;
    }

    [Serializable]
    public class NeutronEditorSettings
    {
        [Range(1, 256)] public int FPS = 60;
    }

    [Serializable]
    public class NeutronServerSettings
    {
        [Range(1, 256)] public int FPS = 45;
        public int BackLog = 10;
        [HideInInspector] public bool NeutronAntiCheat = true;
    }

    [Serializable]
    public class NeutronLagSettings
    {
        [AllowNesting] [ReadOnly] public bool Inbound;
        public bool Outbound;
        [Range(1, 150)] public int InOutDelay = 1;
        public bool Drop;
        [Range(1, 100)] public int Percent = 1;
    }

    [Serializable]
    public class NeutronClientSettings
    {
        [Range(1, 256)] public int FPS = 60;
        [Range(0.1F, 1)] public float PingRate = 0.2F;
        [Range(1, 60)] public float TcpKeepAlive = 5F;
    }

    [Serializable]
    public class NeutronConstantsSettings
    {
        #region String's
        public string ContainerName = "[Container] -> Player[Main]";
        #endregion

        #region Integers
        [Range(1, 1472)]
        public int MaxUdpPacketSize = (int)(0.5 * 1024); // bytes
        [Range(1, 65535)]
        public int MaxTcpPacketSize = 2 * 1024; // bytes
        [Range(1, 65535)]
        public int ReceiveBufferSize = 8 * 1024; // bytes
        [Range(1, 65535)]
        public int SendBufferSize = 8 * 1024; // bytes
        [Range(1, 65535)]
        public int BufferedStreamSize = 8 * 1024; // bytes
        [Range(1, 5)]
        public int MaxConnectionsPerIp = 2;
        public int MaxLatency = 150; // ms
        //////////////////////////////////////////////////
        public const int GENERATE_PLAYER_ID = 0;
        public const int BOUNDED_CAPACITY = int.MaxValue;
        public const int MIN_SEND_RATE = 1;
        public const int MAX_SEND_RATE = 128;
        #endregion

        #region Single's
        public const float ONE_PER_SECOND = 1F;
        #endregion

        #region Double's
        public double TimeDesyncTolerance = 1D;
        public double TimeResyncTolerance = 0.001D;
        #endregion

        #region Others
        public EncodingType Encoding = EncodingType.ASCII;
        public HeaderSizeType HeaderSize = HeaderSizeType.Short;
        #endregion

        #region Bool's
        public bool BufferedStream = false;
        #endregion
    }

    [Serializable]
    public class NeutronDefaultHandlerSettings
    {
        public HandlerOptions OnPlayerNicknameChanged = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerDisconnected = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerCreatedRoom = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerJoinedRoom = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerLeaveRoom = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerLeaveChannel = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerPropertiesChanged = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnRoomPropertiesChanged = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public HandlerOptions OnPlayerDestroyed = new HandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
    }
}