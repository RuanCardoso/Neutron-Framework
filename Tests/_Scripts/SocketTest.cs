using NeutronNetwork.Internal;
using System;
using System.Net;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork.Tests
{
    public class SocketTest : MonoBehaviour
    {
        NeutronUdp Server = new NeutronUdp();
        NeutronUdp Client = new NeutronUdp();

        public int numConnections = 1;
        private void Awake()
        {
            Server.OnMessageCompleted += OnServerMessageCompleted;
            Client.OnMessageCompleted += OnClientMessageCompleted;
#if !UNITY_EDITOR
            Server.Bind(new IPEndPoint(IPAddress.Any, 5055));
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            Client.Bind(new IPEndPoint(IPAddress.Any, Helpers.SocketHelper.GetFreePort(Packets.Protocol.Udp)));
#endif
        }

        private void OnServerMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            switch (udp.OnServerMessageCompleted(stream, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case PacketType.Test:
                    writer.WritePacket((byte)PacketType.Test);
                    writer.Write(reader.ReadInt());
                    udp.SendToClient(stream, channelMode, targetMode, opMode, playerId, endPoint);
                    break;
            }
        }

        private void OnClientMessageCompleted(NeutronStream stream, ushort playerId, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            switch (udp.OnClientMessageCompleted(stream, playerId, endPoint, channelMode, targetMode, opMode, udp))
            {
                case PacketType.Test:
                    Debug.Log("Hehehhe boy: " + reader.ReadInt());
                    break;
            }
        }

        private void Start()
        {
#if !UNITY_EDITOR
            Server.Init();
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            Client.Init();
            StartCoroutine(Client.Connect(pEndPoint));
#endif
        }

        private void OnApplicationQuit()
        {
#if !UNITY_EDITOR
            Server.Close();
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            Client.Close();
#endif
        }

        byte[] buffer = new byte[] { 1, 2, 3, 5, 1, 2, 3/*, 1, 2, 3, 5, 1, 2, 3, 1, 2, 3, 5, 1, 2, 3, 1, 2, 3, 5, 1, 2, 3*/ };

        EndPoint pEndPoint = new NonAllocEndPoint(IPAddress.Loopback, 5055);

        public float Pps = 50;
        float timeToSend;

        int number = 10;
        private void Update()
        {
#if UNITY_EDITOR || !UNITY_SERVER
            // if (Input.GetKeyDown(KeyCode.G))
            // {
            //     unchecked
            //     {
            //         ++number;
            //         LogHelper.Error("number: " + number);
            //     }
            // }
#endif
#if !UNITY_EDITOR

            Server.ReTransmit(Time.deltaTime);
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            Client.ReTransmit(Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Return))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number);
                    Client.SendToServer(stream, ChannelMode.Unreliable, TargetMode.Single);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number);
                    Client.SendToServer(stream, ChannelMode.Reliable, TargetMode.Single);
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number);
                    Client.SendToServer(stream, ChannelMode.ReliableSequenced, TargetMode.Single);
                }
            }
#endif
        }
    }
}