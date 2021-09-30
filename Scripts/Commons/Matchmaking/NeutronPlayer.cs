using NeutronNetwork.Helpers;
using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.IO;
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
        ///* Retorna o identificador dojogador.
        /// </summary>
        public int ID {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        ///* Retorna o nome de seu jogador;
        /// </summary>
        public string Nickname {
            get => _nickname;
            set => _nickname = value;
        }

        /// <summary>
        ///* Retorna o atual canal do jogador.
        /// </summary>
        public NeutronChannel Channel {
            get => _channel;
            set => _channel = value;
        }

        /// <summary>
        ///* Retorna a atual sala do jogador.
        /// </summary>
        public NeutronRoom Room {
            get => _room;
            set => _room = value;
        }

        /// <summary>
        ///* Retorna as propriedades do jogador em Json;
        /// </summary>
        public string Properties {
            get => _properties;
            set => _properties = value;
        }

        /// <summary>
        ///* Retorna o ID do banco de dados do jogador, disponível somente no servidor.
        /// </summary>
        public int DatabaseID {
            get => _databaseId;
            set => _databaseId = value;
        }

        /// <summary>
        ///* Retorna se este jogador é o jogador que representa o servidor.
        /// </summary>
        public bool IsServerPlayer {
            get => ID == 0;
        }

        /// <summary>
        ///* Retorna se este jogador é o dono do Matchmaking atual.
        /// </summary>
        public bool IsMaster {
            get => Matchmaking.Owner.Equals(this);
        }

        /// <summary>
        ///* Retorna o estado da sua conexão com o servidor.
        /// </summary>
        public bool IsConnected {
            get;
            set;
        }

        /// <summary>
        ///* Retorna seu identificador de rede(Player).
        /// </summary>
        public NeutronView NeutronView {
            get;
            set;
        }

        /// <summary>
        ///* Propriedades personalizadas do jogador.
        /// </summary>
        public JObject Get {
            get;
        } = new JObject();

        /// <summary>
        ///* Propriedades personalizadas do jogador, disponível somente ao lado do servidor.
        /// </summary>
        public Dictionary<string, object> Prefs {
            get;
        } = new Dictionary<string, object>();

        /// <summary>
        ///* Seu atual Matchmaking, Sala, Grupo ou Channel.<br/>
        ///* Retorna o ultimo tipo de Matchmaking ingressado.
        /// </summary>
        public INeutronMatchmaking Matchmaking {
            get;
            set;
        }
        #endregion

        #region Properties -> Network
        public TcpClient TcpClient {
            get;
        }
        public UdpClient UdpClient {
            get;
        }
        public Stream NetworkStream {
            get;
        }
        public CancellationTokenSource TokenSource {
            get;
        }
        public StateObject StateObject {
            get;
        } = new StateObject();
        //***********************************************************
        public NeutronEventNoReturn OnDestroy {
            get;
            set;
        }
        #endregion

        public NeutronPlayer() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronPlayer(int id, TcpClient tcpClient, CancellationTokenSource cancellationTokenSource)
        {
            ID = id;
            Nickname = $"Player#{id}";
            //**************************************************************************************************
            TcpClient = tcpClient;
            UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
            UdpClient.Client.ReceiveBufferSize = Helper.GetConstants().Udp.UdpReceiveBufferSize;
            UdpClient.Client.SendBufferSize = Helper.GetConstants().Udp.UdpSendBufferSize;
            //**************************************************************************************************
            NetworkStream = SocketHelper.GetStream(tcpClient);
            StateObject.UdpLocalEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;
            StateObject.TcpRemoteEndPoint = (IPEndPoint)TcpClient.Client.RemoteEndPoint;
            //**************************************************************************************************
            TokenSource = cancellationTokenSource;
        }

        public NeutronPlayer(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("id");
            Nickname = info.GetString("nickname");
            Properties = info.GetString("properties");
            //*********************************************
            Get = JObject.Parse(Properties);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("id", ID);
            info.AddValue("nickname", Nickname);
            info.AddValue("properties", Properties);
        }

        public bool Equals(NeutronPlayer player)
        {
            return this.ID == player.ID;
        }

        public bool Equals(NeutronPlayer x, NeutronPlayer y)
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
            NetworkStream.Dispose();
            using (TokenSource)
            {
                TokenSource.Cancel();
            }
            TcpClient.Dispose();
            UdpClient.Dispose();
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            Title = _nickname;
#endif
        }

        public void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            Title = _nickname;
#endif
        }

        public override bool Equals(object player)
        {
            return ID == ((NeutronPlayer)player).ID;
        }

        public override int GetHashCode()
        {
            return ID.GetHashCode();
        }

        public override string ToString()
        {
            return $"KKKKKKKKKKKKKKKKKKKKKKKKK Mó preguiça de subistituir isso aqui irmão.";
        }
    }
}