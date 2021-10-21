using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
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
        [Range(1, 256)] public int FPS = 128;
        [Range(1, Int16.MaxValue)] public int MaxPlayers = 300;
        [Range(1, 65535)] public int StreamPoolCapacity = 1200;
        [Range(1, 65535)] public int PacketPoolCapacity = 1200;
        public SerializationMode Serialization = SerializationMode.Json;
        public CompressionMode Compression = CompressionMode.None;
        public bool NoDelay = true;
        [HideInInspector] public bool PerfomanceMode;
    }

    [Serializable]
    public class NeutronServerSettings
    {
        public int BackLog = 10;
        [HideInInspector] public bool NeutronAntiCheat = true;
    }

    [Serializable]
    public class NeutronClientSettings
    {
        [Range(0.1F, 5F)] public float UdpKeepAlive = 2F;
        [Range(1F, 15F)] public float TcpKeepAlive = 5F;
    }

    [Serializable]
    public class NeutronConstantsSettings
    {
        #region Integers
        [Range(1, 5)]
        public int MaxConnectionsPerIp = 2;
        [Range(1, 150)]
        public int MaxLatency = 150; // ms
        //---------------------------------------------------
        public const int GENERATE_PLAYER_ID = 0;
        public const int TIME_DECIMAL_PLACES = 3;
        public const int MIN_SEND_RATE = 1;
        public const int MAX_SEND_RATE = 128;
        #endregion

        #region Single's
        public const float ONE_PER_SECOND = 1F;
        #endregion

        #region Double's
        [Range(0, 5)]
        public double TimeDesyncTolerance = 1D;
        [Range(0, 1)]
        public double TimeResyncTolerance = 0.001D;
        #endregion

        #region Others
        [NonSerialized] public ThreadType ReceiveThread = ThreadType.Neutron;
        [NonSerialized] [AllowNesting] public ThreadType PacketThread = ThreadType.Neutron;
        public ReceiveType ReceiveModel = ReceiveType.Asynchronous;
        public SendType SendModel = SendType.Synchronous;
        [ShowIf("SendModel", SendType.Asynchronous)] [AllowNesting] public AsynchronousType SendAsyncPattern = AsynchronousType.APM;
        public AsynchronousType ReceiveAsyncPattern = AsynchronousType.APM;
        public EncodingType Encoding = EncodingType.ASCII;
        public HeaderSizeType HeaderSize = HeaderSizeType.Short;
        #endregion

        public TcpOptions Tcp = new TcpOptions();
        public UdpOptions Udp = new UdpOptions();
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

    [Serializable]
    public class TcpOptions
    {
        [Range(1, 65535)]
        public int MaxTcpPacketSize = 2 * 1024; // bytes
        [Range(1, 65535)]
        public int TcpReceiveBufferSize = 8 * 1024; // bytes
        [Range(1, 65535)]
        public int TcpSendBufferSize = 8 * 1024; // bytes
        public bool BufferedStream = false;
        [ShowIf("BufferedStream")] [AllowNesting] public int BufferedStreamSize = 8 * 1024; // bytes
    }

    [Serializable]
    public class UdpOptions
    {
        [Range(1, 1472)]
        public int MaxUdpPacketSize = (int)(0.5 * 1024); // bytes
        [Range(1, 65535)]
        public int UdpReceiveBufferSize = 8 * 1024; // bytes
        [Range(1, 65535)]
        public int UdpSendBufferSize = 8 * 1024; // bytes
    }
}