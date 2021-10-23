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
    public class NeutronPlayer : INeutronIdentify, INeutronSerializable, IDisposable, ISerializationCallbackReceiver, IEquatable<NeutronPlayer>, IEqualityComparer<NeutronPlayer>
    {
        [SerializeField]
        [HideInInspector]
        private bool _isInitialized;
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052

        #region Default Values
        private const string DEFAULT_PROPERTIES = "{\"Team\":\"Neutron\"}";
        #endregion

        #region Fields
        [SerializeField] [AllowNesting] [ReadOnly] private int _id;
        [SerializeField] private string _nickname = string.Empty;
        [NonSerialized] private NeutronChannel _channel;
        [NonSerialized] private NeutronRoom _room;
        [SerializeField] [ResizableTextArea] private string _properties = DEFAULT_PROPERTIES;
        [SerializeField] [AllowNesting] [ReadOnly] private int _databaseId;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador do jogador.
        /// </summary>
        public int Id {
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
            set {
                _properties = value;
                try
                {
                    if (!string.IsNullOrEmpty(value))
                        Get = JObject.Parse(value);
                }
                catch { LogHelper.Error("Invalid json in properties."); }
            }
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
            get => Id == 0;
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
            private set;
        } = new JObject();

        /// <summary>
        ///* Propriedades personalizadas do jogador, disponível somente ao lado do servidor.
        /// </summary>
        public Dictionary<string, object> Prefs {
            get;
        } = new Dictionary<string, object>();

        /// <summary>
        ///* Seu atual Matchmaking, Sala ou Channel.<br/>
        ///* Retorna o último tipo de Matchmaking ingressado.
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

        public NeutronEventNoReturn OnDestroy {
            get;
            set;
        }

        public short SceneObjectId {
            get;
            set;
        }
        #endregion

        public NeutronPlayer() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronPlayer(int id, TcpClient tcpClient, CancellationTokenSource cancellationTokenSource)
        {
            Id = id;
            Nickname = $"Player#{id}";
            TcpClient = tcpClient;
            UdpClient = new UdpClient(new IPEndPoint(IPAddress.Any, SocketHelper.GetFreePort(Protocol.Udp)));
            UdpClient.Client.ReceiveBufferSize = Helper.GetConstants().Udp.UdpReceiveBufferSize;
            UdpClient.Client.SendBufferSize = Helper.GetConstants().Udp.UdpSendBufferSize;
            NetworkStream = SocketHelper.GetStream(tcpClient);
            StateObject.UdpLocalEndPoint = (IPEndPoint)UdpClient.Client.LocalEndPoint;
            StateObject.TcpRemoteEndPoint = (IPEndPoint)TcpClient.Client.RemoteEndPoint;
            TokenSource = cancellationTokenSource;
        }

        public NeutronPlayer(SerializationInfo info, StreamingContext context)
        {
            Id = info.GetInt32("id");
            Nickname = info.GetString("nickname");
            Properties = info.GetString("properties");
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("id", Id);
            info.AddValue("nickname", Nickname);
            info.AddValue("properties", Properties);
        }

        public void Apply(NeutronPlayer player)
        {
            _id = player.Id;
            _nickname = player.Nickname;
            Properties = player.Properties;
        }

        public bool Equals(NeutronPlayer player)
        {
            return this.Id == player.Id;
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
            return x.Id == y.Id;
        }

        public Int32 GetHashCode(NeutronPlayer obj)
        {
            return obj.Id.GetHashCode();
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
            if (!_isInitialized)
            {
                _properties = DEFAULT_PROPERTIES;
                _isInitialized = true;
            }
#endif
        }

        public override string ToString()
        {
            return $"KKKKKKKKKKKKKKKKKKKKKKKKK Mó preguiça de subistituir isso aqui irmão.";
        }
    }
}