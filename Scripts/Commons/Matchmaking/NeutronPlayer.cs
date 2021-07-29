using NeutronNetwork.Helpers;
using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Json;
using NeutronNetwork.Naughty.Attributes;
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
    public class NeutronPlayer : INeutron, INeutronSerializable, IDisposable, ISerializationCallbackReceiver, IEquatable<NeutronPlayer>, IEqualityComparer<NeutronPlayer>
    {
#if UNITY_EDITOR
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052
#endif
        #region Fields
        [SerializeField] [AllowNesting] [ReadOnly] private int _id;
        [SerializeField] private string _nickname = string.Empty;
        [NonSerialized] private NeutronChannel _channel;
        [NonSerialized] private NeutronRoom _room;
        [SerializeField] [ResizableTextArea] private string _properties = "{\"Neutron\":\"Neutron\"}";
        [SerializeField] [AllowNesting] [ReadOnly] private int _databaseId;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador da sala.
        /// </summary>
        public int ID { get => _id; set => _id = value; }
        /// <summary>
        ///* Retorna o nome de seu jogador;
        /// </summary>
        public string Nickname { get => _nickname; set => _nickname = value; }
        /// <summary>
        ///* Retorna o atual canal do jogador.
        /// </summary>
        public NeutronChannel Channel { get => _channel; set => _channel = value; }
        /// <summary>
        ///* Retorna a atual sala do jogador.
        /// </summary>
        public NeutronRoom Room { get => _room; set => _room = value; }
        /// <summary>
        ///* Retorna as propriedades do jogador em Json;
        /// </summary>
        public string Properties { get => _properties; set => _properties = value; }
        /// <summary>
        ///* Retorna o ID do banco de dados do jogador, disponível somente no servidor.
        /// </summary>
        public int DatabaseID { get => _databaseId; set => _databaseId = value; }
        /// <summary>
        ///* Retorna se este jogador pertence ao servidor.
        /// </summary>
        public bool IsServer { get; set; }
        /// <summary>
        ///* Retorna o estado da sua conexão com o servidor.
        /// </summary>
        public bool IsConnected { get; set; }
        /// <summary>
        ///* Retorna seu identificador de rede.
        /// </summary>
        public NeutronView NeutronView { get; set; }
        /// <summary>
        ///* Propriedades personalizades do jogador.
        /// </summary>
        public Dictionary<string, object> Get { get; set; }
        /// <summary>
        ///* Seu atual Matchmaking, Sala, Grupo ou Channel.<br/>
        ///* Retorna o ultimo tipo de Matchmaking ingressado.
        /// </summary>
        public INeutronMatchmaking Matchmaking { get; set; }
        #endregion

        #region Properties -> Network
        public IPEndPoint RemoteEndPoint { get; set; }
        public IPEndPoint LocalEndPoint { get; set; }
        public TcpClient TcpClient { get; set; }
        public UdpClient UdpClient { get; set; }
        #endregion

        #region Threading
        public CancellationTokenSource TokenSource { get; set; }
        #endregion

        public NeutronPlayer() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronPlayer(int id, TcpClient tcpClient, CancellationTokenSource cancellationTokenSource)
        {
            #region Properties
            ID = id;
            Nickname = PlayerHelper.GenerateNickname(id);
            #endregion

            #region Socket
            TcpClient = tcpClient;
            UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
            LocalEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;
            #endregion

            #region Others
            TokenSource = cancellationTokenSource;
            #endregion
        }

        public NeutronPlayer(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Nickname = info.GetString("NN");
            //_currentChannel = info.GetInt32("CC");
            //CurrentRoom = info.GetInt32("CR");
            Properties = info.GetString("_");
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NN", Nickname);
            //info.AddValue("CC", CurrentChannel);
            //info.AddValue("CR", CurrentRoom);
            info.AddValue("_", Properties);
        }

        public Boolean Equals(NeutronPlayer player)
        {
            return this.ID == player.ID;
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
            TokenSource.Dispose();
            TcpClient.Dispose();
            UdpClient.Dispose();
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            Title = _nickname;
#endif
        }
    }
}