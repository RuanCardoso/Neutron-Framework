using NeutronNetwork.Internal;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork.Tests
{
    public class SocketTest : MonoBehaviour
    {
        NeutronUdp Server = new NeutronUdp();
        NeutronUdp Client = new NeutronUdp();

        private List<int> receivedNumbers1 = new List<int>();
        private List<int> receivedNumbers2 = new List<int>();
        private List<int> receivedNumbers3 = new List<int>();

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
            Console.Clear();
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
                    {
                        switch (channelMode)
                        {
                            case ChannelMode.Unreliable:
                                receivedNumbers1.Add(reader.ReadInt());
                                break;
                            case ChannelMode.Reliable:
                                receivedNumbers2.Add(reader.ReadInt());
                                break;
                            case ChannelMode.ReliableSequenced:
                                receivedNumbers3.Add(reader.ReadInt());
                                break;
                        }
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
            StartCoroutine(Client.Connect(pEndPoint));
#endif
            Console.Clear();
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

        int number1 = 0;
        int number2 = 0;
        int number3 = 0;
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
#if !UNITY_EDITOR && UNITY_SERVER

            Server.ReTransmit(Time.deltaTime);
#endif
#if UNITY_EDITOR || !UNITY_SERVER
            Client.ReTransmit(Time.deltaTime);
            if (Input.GetKeyDown(KeyCode.Return))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number1);
                    Client.SendToServer(stream, ChannelMode.Unreliable, TargetMode.Single);
                }
            }

            if (Input.GetKeyDown(KeyCode.R))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number2);
                    Client.SendToServer(stream, ChannelMode.Reliable, TargetMode.Single);
                }
            }

            if (Input.GetKeyDown(KeyCode.S))
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    stream.Writer.WritePacket((byte)PacketType.Test);
                    stream.Writer.Write(++number3);
                    Client.SendToServer(stream, ChannelMode.ReliableSequenced, TargetMode.Single);
                }
            }

            if (Input.GetKeyDown(KeyCode.M))
            {
                int[] listsOfTests = { number1, number2, number3 };
                string[] listsOfTestsStrings = { "Unreliable", "Reliable", "RealibleSequenced" };
                List<List<int>> listsOfTestsList = new() { receivedNumbers1, receivedNumbers2, receivedNumbers3 };
                for (int i = 0; i < listsOfTests.Length; i++)
                {
                    int number = listsOfTests[i];
                    if (number > 0)
                    {
                        var missingNumbers = Enumerable.Range(1, number).Except(listsOfTestsList[i]);
                        if (missingNumbers.Count() > 0)
                            LogHelper.Error($"{listsOfTestsStrings[i]} -> Failed Packet: " + string.Join(", ", missingNumbers));
                        var numbers = Enumerable.Range(1, number).Except(missingNumbers);
                        if (numbers.Count() > 0)
                        {
                            LogHelper.Info($"{listsOfTestsStrings[i]} -> Success Packet: " + string.Join(", ", numbers));
                            LogHelper.Info($"n: {string.Join(", ", listsOfTestsList[i])}");
                            // if(i == 2)
                        }
                    }
                }
            }
#endif
        }
    }
}