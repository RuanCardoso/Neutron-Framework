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
        [Range(1, Int16.MaxValue)] public int MaxPlayers = 300;
        [Range(1, 60)] public int ActionsProcessedPerFrame = 1;
        public Serialization Serialization = Serialization.Json;
        public Compression Compression = Compression.None;
        public bool NoDelay = true;
        [ReadOnly] [AllowNesting] public bool Lan;
        [ReadOnly] [AllowNesting] public bool P2P;
    }

    [Serializable]
    public class NeutronEditorSettings
    {
        [Range(1, 256)] public int FPS = 60;
    }

    [Serializable]
    public class NeutronServerSettings
    {
        [Range(1, 256)] public int FPS = 128;
        [Range(1, 30)] public int PacketsProcessedPerTick = 1;
        public int BackLog = 10;
        [HideInInspector] public bool NeutronAntiCheat = true;
    }

    [Serializable]
    public class NeutronLagSettings
    {
        [AllowNesting] [ReadOnly] public bool Inbound;
        public bool Outbound;
        [Range(1, 150)] public int InOutDelay;
        public bool Drop;
        [Range(1, 100)] public int Percent;
    }

    [Serializable]
    public class NeutronClientSettings
    {
        [Range(1, 256)] public int FPS = 90;
        [Range(0.1F, 1)] public float PingRate = 0.2F;
    }

    [Serializable]
    public class NeutronDefaultHandlerSettings
    {
        public NeutronDefaultHandlerOptions OnPlayerNicknameChanged = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerDisconnected = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerCreatedRoom = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerJoinedRoom = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerLeaveRoom = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerLeaveChannel = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerPropertiesChanged = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnRoomPropertiesChanged = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
        public NeutronDefaultHandlerOptions OnPlayerDestroyed = new NeutronDefaultHandlerOptions(TargetTo.All, TunnelingTo.Auto, Protocol.Tcp);
    }
}