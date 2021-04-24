using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Wrappers;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class Player : INeutronNotify, INeutronSerializable, IDisposable, IEquatable<Player>, IEqualityComparer<Player>
    {
        /// <summary>
        /// ID of player.
        /// </summary>
        public int ID { get => iD; set => iD = value; }
        [SerializeField] [ReadOnly] private int iD;
        /// <summary>
        /// Nickname of player.
        /// </summary>
        public string Nickname { get => nickname; set => nickname = value; }
        [SerializeField] private string nickname = string.Empty;
        /// <summary>
        /// Current channel of player.
        /// </summary>
        public int CurrentChannel { get => currentChannel; set => currentChannel = value; }
        [SerializeField, ReadOnly] private int currentChannel = -1;
        /// <summary>
        /// Current room of player.
        /// </summary>
        public int CurrentRoom { get => currentRoom; set => currentRoom = value; }
        [SerializeField, ReadOnly] private int currentRoom = -1;
        /// <summary>
        /// Check if player is a bot.
        /// </summary>
        public bool IsBot { get => isBot; set => isBot = value; }
        [SerializeField, ReadOnly] private bool isBot;
        /// <summary>
        /// Properties of player.
        /// </summary>
        public string _ { get => m_Properties; set => m_Properties = value; }
        [SerializeField] private string m_Properties = "{\"Neutron\":\"Neutron\"}";
        /// <summary>
        /// ID of database.
        /// </summary>
        public int DatabaseID { get => databaseID; set => databaseID = value; }
        [ReadOnly] private int databaseID;
        /// <summary>
        /// state of player.
        /// </summary>
        public NeutronView NeutronView { get; set; }
        /// <summary>
        /// Properties of player.
        /// </summary>
        public Dictionary<string, object> Get { get; set; }
        /// <summary>
        /// Check if this player is a server Player.
        /// </summary>
        public bool isServer { get; set; }
        /// <summary>
        /// queue of data TCP.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public NeutronQueue<DataBuffer> qData = new NeutronQueue<DataBuffer>();
        /// <summary>
        /// Remote EndPoint of player.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public IPEndPoint rPEndPoint;
        /// <summary>
        /// Local EndPoint of player.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public IPEndPoint lPEndPoint;
        /// <summary>
        /// Socket.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public TcpClient tcpClient;
        /// <summary>
        /// Socket.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public UdpClient udpClient;
        /// <summary>
        /// cts.
        /// </summary>
        public CancellationTokenSource _cts;
        /// <summary>
        /// infor.
        /// </summary>
        [NonSerialized] public NeutronMessageInfo infor;

        public Player() { }// the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Player(int ID, TcpClient tcpClient, CancellationTokenSource _cts)
        {
            string randomNickname = $"Unk#{new System.Random().Next(0, 10000)}";
            IPEndPoint localIPEndPoint = new IPEndPoint(IPAddress.Any, InternalUtils.GetFreePort(Protocol.Udp));
            this.ID = ID;
            this.Nickname = randomNickname;
            this.tcpClient = tcpClient;
            this.udpClient = new UdpClient(localIPEndPoint);
            this.lPEndPoint = (IPEndPoint)udpClient.Client.LocalEndPoint;
            this.infor = new NeutronMessageInfo(0);
            this._cts = _cts;
        }

        public Player(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Nickname = info.GetString("NN");
            currentChannel = info.GetInt32("CC");
            CurrentRoom = info.GetInt32("CR");
            IsBot = info.GetBoolean("IB");
            _ = info.GetString("_");
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(_);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NN", Nickname);
            info.AddValue("CC", CurrentChannel);
            info.AddValue("CR", CurrentRoom);
            info.AddValue("IB", IsBot);
            info.AddValue("_", _);
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