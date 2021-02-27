using NeutronNetwork;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class Player : IEquatable<Player>, INotify, IEqualityComparer<Player>, IDisposable
    {
        /// <summary>
        /// ID of player.
        /// </summary>
        public int ID { get => iD; set => iD = value; }
        [SerializeField] private int iD;
        /// <summary>
        /// Nickname of player.
        /// </summary>
        public string Nick { get => nick; set => nick = value; }
        [SerializeField, ReadOnly] private string nick;
        /// <summary>
        /// Current channel of player.
        /// </summary>
        public int CurrCh { get => currCh; set => currCh = value; }
        [SerializeField, ReadOnly] private int currCh = -1;
        /// <summary>
        /// Current room of player.
        /// </summary>
        public int CurrRoom { get => currRoom; set => currRoom = value; }
        [SerializeField, ReadOnly] private int currRoom = -1;
        /// <summary>
        /// Check if player is a bot.
        /// </summary>
        public bool IsBot { get => isBot; set => isBot = value; }
        [SerializeField, ReadOnly] private bool isBot;
        /// <summary>
        /// Properties of player.
        /// </summary>
        public string _ { get => props; set => props = value; }
        [SerializeField, TextArea] private string props = string.Empty;
        /// <summary>
        /// ID of database.
        /// </summary>
        [JsonIgnore]
        public int DID { get => dID; set => dID = value; }
#if !UNITY_EDITOR
        [NonSerialized]
#else
        [SerializeField]
#endif
        [ReadOnly] private int dID;
        /// <summary>
        /// state of player.
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public NeutronView NeutronView { get; set; }
        /// <summary>
        /// Properties of player.
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public Dictionary<string, object> GetProps { get; set; }
        /// <summary>
        /// queue of data TCP.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        [NonSerialized] public NeutronQueue<DataBuffer> qData = new NeutronQueue<DataBuffer>();
        /// <summary>
        /// Remote EndPoint of player.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        [NonSerialized] public IPEndPoint rPEndPoint;
        /// <summary>
        /// Local EndPoint of player.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        [NonSerialized] public IPEndPoint lPEndPoint;
        /// <summary>
        /// Socket.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        [NonSerialized] public TcpClient tcpClient;
        /// <summary>
        /// Socket.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        [NonSerialized] public UdpClient udpClient;
        /// <summary>
        /// cts.
        /// </summary>
        [NonSerialized] public CancellationTokenSource _cts;

        public Player() { }// the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Player(int ID, TcpClient tcpClient, CancellationTokenSource _cts)
        {
            this.ID = ID;
            this.Nick = $"Unknown#{new System.Random().Next(0, 100000)}";
            this.tcpClient = tcpClient;
            this.udpClient = new UdpClient(new IPEndPoint(IPAddress.Any, Utils.GetFreePort(Protocol.Udp)));
            this.lPEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            this._cts = _cts;
        }

        public Boolean Equals(Player other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(Player x, Player y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (object.ReferenceEquals(x, null) ||
                object.ReferenceEquals(y, null))
            {
                return false;
            }
            return x.ID == y.ID;
        }

        public Int32 GetHashCode(Player obj)
        {
            return obj.ID.GetHashCode();
        }

        public void Dispose()
        {
            using (_cts)
            {
                using (udpClient)
                {
                    using (tcpClient)
                    {
                        _cts.Cancel();
                    }
                }
            }
        }
    }
}