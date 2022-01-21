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

        private void OnServerMessageCompleted(NeutronStream stream, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            var writer = stream.Writer;
            switch ((PacketType)reader.ReadPacket())
            {
                case PacketType.Connect:
                    {
                        ushort playerId = (ushort)UnityEngine.Random.Range(0, ushort.MaxValue);
                        writer.Write(playerId);
                        IPEndPoint iPEndPoint = (IPEndPoint)endPoint;
#pragma warning disable 618
                        udp.Clients.TryAdd(endPoint, new(playerId, iPEndPoint.Address.Address, iPEndPoint.Port));
#pragma warning restore 618
                    }
                    break;
            }
        }

        private void OnClientMessageCompleted(NeutronStream stream, EndPoint endPoint, ChannelMode channelMode, TargetMode targetMode, OperationMode opMode, NeutronUdp udp)
        {
            var reader = stream.Reader;
            switch ((PacketType)reader.ReadPacket())
            {
                case PacketType.Connect:
                    {
                        LogHelper.Error(reader.ReadUShort());
                    }
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
            Client.Connect(pEndPoint);
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

        int number = 0;
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

            //Server.ReTransmit(Time.deltaTime);
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            //Client.ReTransmit(Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Return))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.Write(++number);
                    Client.Send(stream, ChannelMode.ReliableSequenced, TargetMode.Single);
                }
            }
#endif
        }
    }
}