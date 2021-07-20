using NeutronNetwork.Attributes;
using NeutronNetwork.Helpers;
using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Server.Internal;
using NeutronNetwork.Json;
using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Constants;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronPlayer : INeutron, INeutronSerializable, IDisposable, IEquatable<NeutronPlayer>, IEqualityComparer<NeutronPlayer>
    {
        /// <summary>
        /// ID of player.
        /// </summary>
        public int ID { get => m_ID; set => m_ID = value; }
        [SerializeField] [ReadOnly] private int m_ID;
        /// <summary>
        /// Nickname of player.
        /// </summary>
        public string Nickname { get => nickname; set => nickname = value; }
        [SerializeField] private string nickname = string.Empty;
        /// <summary>
        /// Current channel of player.
        /// </summary>
        public int CurrentChannel { get => currentChannel; set => currentChannel = value; }
        [SerializeField] [ReadOnly] private int currentChannel = -1;
        /// <summary>
        /// Current room of player.
        /// </summary>
        public int CurrentRoom { get => currentRoom; set => currentRoom = value; }
        [SerializeField] [ReadOnly] private int currentRoom = -1;
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
        public bool IsServer { get; set; }
        /// <summary>
        /// Check if this player is connected in server.
        /// </summary>
        public bool IsConnected { get; set; }
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
        public TcpClient m_TcpClient;
        /// <summary>
        /// Socket.
        /// returns null on the client.
        /// not serialized over the network.
        /// </summary>
        public UdpClient m_UdpClient;
        /// <summary>
        /// cts.
        /// </summary>
        public CancellationTokenSource m_CTS;

        #region Interfaces
        public INeutronMatchmaking Matchmaking;
        #endregion

        public NeutronPlayer() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronPlayer(int ID)
        {
            this.ID = ID;
        }

        public NeutronPlayer(int ID, TcpClient tcpClient, CancellationTokenSource _cts)
        {
            #region Properties
            this.ID = ID;
            this.Nickname = PlayerHelper.GetNickname(ID);
            #endregion

            #region Socket
            this.m_TcpClient = tcpClient;
            this.m_UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
            this.lPEndPoint = (IPEndPoint)m_UdpClient.Client.LocalEndPoint;
            #endregion

            #region Others
            this.m_CTS = _cts;
            #endregion
        }

        public NeutronPlayer(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Nickname = info.GetString("NN");
            currentChannel = info.GetInt32("CC");
            CurrentRoom = info.GetInt32("CR");
            _ = info.GetString("_");
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(_);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NN", Nickname);
            info.AddValue("CC", CurrentChannel);
            info.AddValue("CR", CurrentRoom);
            info.AddValue("_", _);
        }

        public Boolean Equals(NeutronPlayer other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(NeutronPlayer x, NeutronPlayer y)
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

        public Int32 GetHashCode(NeutronPlayer obj)
        {
            return obj.ID.GetHashCode();
        }

        public void Dispose()
        {
            m_CTS.Dispose();
            m_TcpClient.Dispose();
            m_UdpClient.Dispose();
        }
    }
}