using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;

namespace NeutronNetwork.Internal.Client
{
    public class NeutronClientConstants : MonoBehaviour
    {   // It inherits from MonoBehaviour because it is an instance of GameObject.
        //-------------------------------------------------------------------------------------------------------------
        public NeutronQueue<Action> mainThreadActions;
        public NeutronQueue<Action> monoBehaviourRPCActions;
        //-------------------------------------------------------------------------------------------------------------
        protected TcpClient _TCPSocket;
        protected UdpClient _UDPSocket;
        //-------------------------------------------------------------------------------------------------------------
        protected float tInputDelay = 0f;
        protected float tNetworkStatsDelay = 0f;
        //-------------------------------------------------------------------------------------------------------------
        public const float navMeshTolerance = 15f;
        //-------------------------------------------------------------------------------------------------------------
        protected long ping = 0;
        protected double packetLoss = 0;
        protected int pingAmount = 0;
        //-------------------------------------------------------------------------------------------------------------
        //-------------------------------------------------------------------------------------------------------------
        public ConcurrentDictionary<int, NeutronView> networkObjects;
        public ConcurrentDictionary<int, object[]> properties;
        //-------------------------------------------------------------------------------------------------------------
        protected IPEndPoint endPointUDP;
        /// <summary>
        /// Cancellation token.
        /// </summary>
        protected CancellationTokenSource _cts = new CancellationTokenSource();

        public void Internal()
        {
            DontDestroyOnLoad(gameObject);
            NeutronConfig.LoadSettings();
            mainThreadActions = new NeutronQueue<Action>();
            monoBehaviourRPCActions = new NeutronQueue<Action>();
            //-------------------------------------------------------------------------------------------------------------
            _TCPSocket = new TcpClient(new IPEndPoint(IPAddress.Any, Utils.GetFreePort(Protocol.Tcp)));
            _UDPSocket = new UdpClient(new IPEndPoint(IPAddress.Any, Utils.GetFreePort(Protocol.Udp)));
            //-------------------------------------------------------------------------------------------------------------
            //-------------------------------------------------------------------------------------------------------------
            networkObjects = new ConcurrentDictionary<int, NeutronView>();
            properties = new ConcurrentDictionary<int, object[]>();
        }

        public void Dispose()
        {
            try
            {
                _cts.Cancel();
                _cts.Dispose();
                _TCPSocket.Close();
                _UDPSocket.Close();
            }
            catch { }
        }

        private void OnApplicationQuit()
        {
            Dispose();
        }
    }
}