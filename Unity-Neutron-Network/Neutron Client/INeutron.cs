using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Client.InternalEvents;
using NeutronNetwork.Internal.Attributes;
using System.IO;

namespace NeutronNetwork
{
    public class Neutron : NeutronCFunc
    {
        /// <summary>
        /// Returns instance of server
        /// </summary>
        public static NeutronServer Server {
            get {
                if (NeutronSFunc._ != null) return (NeutronServer)NeutronSFunc._;
#if !UNITY_EDITOR && !UNITY_SERVER
                Utils.LoggerError("You cannot access server functions on the client outside of Unity Editor.");
#endif
                return null;
            }
        }
        /// <summary>
        /// Returns the local Client.
        /// </summary>
        public static Neutron Client { get; private set; }
        /// <summary>
        /// Returns true if the player is a bot.
        /// </summary>
        public bool IsBot { get => _isBot; }
        /// <summary>
        /// Returns the Player Object.
        /// </summary>
        public NeutronView NeutronView { get; set; }
        /// <summary>
        /// Returns to the local player's instance.
        /// </summary>
        public Player MyPlayer { get => _myPlayer; }
        /// <summary>
        /// Returns true if connected.
        /// </summary>
        public bool IsConnected { get; private set; }
        /// <summary>
        /// Get/Set field private.
        /// </summary>
        private string _nickname;
        /// <summary>
        /// The nickname of the player that will be used to be displayed to other players.
        /// </summary>
        public string Nickname {
            get => _nickname;
            set {
                if (string.IsNullOrEmpty(value)) Utils.LoggerError("Nick not allowed");
                else
                    SetNickname(value);
            }
        }

        /// <summary>
        /// This event is called when your connection to the server is established or fails.
        /// </summary>
        public event Events.OnNeutronConnected OnNeutronConnected;
        /// <summary>
        /// This event is triggered when a player is instantiated.
        /// </summary>
        public event Events.OnPlayerInstantiated OnPlayerInstantiated;
        /// <summary>
        /// This event is called when your connection to the server fails.
        /// </summary>
        public event Events.OnNeutronDisconnected OnNeutronDisconnected;
        /// <summary>
        /// This event is called when you receive a message from yourself or other players.
        /// </summary>
        public event Events.OnMessageReceived OnMessageReceived;
        /// <summary>
        /// This function is called when processing database actions on the server.
        /// </summary>
        public event Events.OnDatabasePacket OnDatabasePacket;
        /// <summary>
        /// This event is called after receiving the channel list from the server.
        /// </summary>
        public event Events.OnChannelsReceived OnChannelsReceived;
        /// <summary>
        ///  This event is called after receiving the rooms list from the server.
        /// </summary>
        public event Events.OnRoomsReceived OnRoomsReceived;
        /// <summary>
        /// This event is triggered when your or other players join the channel.
        /// </summary>
        public event Events.OnPlayerJoinedChannel OnPlayerJoinedChannel;
        /// <summary>
        /// This event is triggered when your or other players left the channel.
        /// </summary>
        public event Events.OnPlayerLeftChannel OnPlayerLeftChannel;
        /// <summary>
        /// This event is triggered when your or other players join the room.
        /// </summary>
        public event Events.OnPlayerJoinedRoom OnPlayerJoinedRoom;
        /// <summary>
        /// This event is triggered when your or other players left the room.
        /// </summary>
        public event Events.OnPlayerLeftRoom OnPlayerLeftRoom;
        /// <summary>
        /// This event is triggered when your create a room.
        /// </summary>
        public event Events.OnCreatedRoom OnCreatedRoom;
        /// <summary>
        /// This event is triggered when your get an error.
        /// </summary>
        public event Events.OnFailed OnFailed;
        /// <summary>
        /// This event is triggered when your player is destroyed.
        /// </summary>
        public event Events.OnDestroyed OnPlayerDestroyed;
        /// <summary>
        /// This event is triggered when your nickname is changed.
        /// </summary>
        public event Events.OnNicknameChanged OnNicknameChanged;
        /// <summary>
        /// This event is triggered when your or other players change their properties.
        /// </summary>
        public event Events.OnPlayerPropertiesChanged OnPlayerPropertiesChanged;
        /// <summary>
        /// This event is triggered when room properties changed.
        /// </summary>
        public event Events.OnRoomPropertiesChanged OnRoomPropertiesChanged;
        /// <summary>
        /// Cancellation token.
        /// </summary>
        private CancellationTokenSource _cts = new CancellationTokenSource();

        private void Update()
        {
#if !UNITY_SERVER
            if (Server == null)
                Application.targetFrameRate = IData.clientFPS;
#endif
            if (!IsConnected) return;

            Utils.Dequeue(ref mainThreadActions, IData.clientDPF);
            Utils.Dequeue(ref monoBehaviourRPCActions, IData.clientDPF);
        }

        /// <summary>
        /// This function will trigger the OnPlayerConnected callback.
        /// Do not call this function in Awake.
        /// </summary>
        /// <param name="ipAddress">The ip of server to connect</param>
        public async void Connect()
        {
            try
            {
                if (!IsBot)
                {
#if UNITY_SERVER
            Utils.LoggerError($"MainClient disabled in server!\r\n");
            return;
#endif
                }
                else if (IsBot)
                {
#if !UNITY_SERVER && !UNITY_EDITOR
            Utils.LoggerError($"Bots disabled in client!");
            return;
#endif
                }

                IData = Data.LoadSettings(); // load settings
                if (!Utils.LoggerError("Failed to load settings.", IData)) return;
                if (IData.ipAddress.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase)) IData.ipAddress = "127.0.0.1";
                COMPRESSION_MODE = (Compression)IData.compressionOptions;
                Internal(); // initialize cliente.
                if (!IsConnected)
                {
                    //////////////////////////////////////////////////////////////////////////////////
#if !UNITY_SERVER
                    Utils.Logger("Wait, connecting to the server.");
#endif
                    //////////////////////////////////////////////////////////////////////////////////
                    await _TCPSocket.ConnectAsync(IData.ipAddress, IData.serverPort); // await connection.
                    if (_TCPSocket.Connected)
                    {
                        IsConnected = true;
                        InitConnect();
                        ThreadPool.QueueUserWorkItem((e) =>
                        {
                            ReadTCPData(e);
                            ReadUDPData(e);
                        }, _cts.Token);
                    }
                    else if (!_TCPSocket.Connected)
                    {
                        IsConnected = false;
                        Utils.LoggerError("Unable to connect to the server");
                        new Action(() => OnNeutronConnected?.Invoke(false, this)).ExecuteOnMainThread(this);
                    }
                }
                else
                {
                    IsConnected = false;
                    Utils.LoggerError("Connection Refused!");
                }
            }
            catch (Exception ex)
            {
                IsConnected = false;
                Utils.StackTrace(ex);
                new Action(() => OnNeutronConnected?.Invoke(false, this)).ExecuteOnMainThread(this);
            }
        }

        private async void ReadUDPData(object obj)
        {
            CancellationToken token = (CancellationToken)obj;
            do
            {
                UdpReceiveResult udpReceiveResult = await _UDPSocket.ReceiveAsync();
                if (udpReceiveResult.Buffer.Length > 0)
                {
                    if (COMPRESSION_MODE == Compression.None) // client
                        ProcessClientData(udpReceiveResult.Buffer); // client
                    else // client
                    {
                        byte[] bBuffer = udpReceiveResult.Buffer.Decompress(COMPRESSION_MODE);
                        ProcessClientData(bBuffer);
                    }
                }

            } while (!token.IsCancellationRequested);
        }

        private async void ReadTCPData(object obj) // client
        {
            byte[] messageLenBuffer = new byte[sizeof(int)];
            try
            {
                CancellationToken token = (CancellationToken)obj;

                using (var netStream = _TCPSocket.GetStream()) // client
                using (var buffStream = new BufferedStream(netStream, Communication.BUFFER_SIZE))
                    do
                    {
                        if (await Communication.ReadAsyncBytes(buffStream, messageLenBuffer, 0, sizeof(int)))
                        {
                            int fixedLength = BitConverter.ToInt32(messageLenBuffer, 0);
                            byte[] messageBuffer = new byte[fixedLength + sizeof(int)];
                            if (await Communication.ReadAsyncBytes(buffStream, messageBuffer, sizeof(int), fixedLength))
                            {
                                using (NeutronReader messageReader = new NeutronReader(messageBuffer, sizeof(int), fixedLength))
                                {
                                    if (COMPRESSION_MODE == Compression.None) // client
                                        ProcessClientData(messageReader.ToArray()); // client
                                    else // client
                                    {
                                        byte[] bBuffer = messageReader.ToArray().Decompress(COMPRESSION_MODE);
                                        ProcessClientData(bBuffer);
                                    }
                                }
                            }
                        }
                        else
                        {
                            _TCPSocket.Close();
                            _cts.Cancel();
                        }
                    } while (!token.IsCancellationRequested);
            }
            catch (Exception ex) { Utils.StackTrace(ex); }
        }

        public void GetNetworkStats(out long Ping, out double PcktLoss, float delay)
        {
            tNetworkStatsDelay += Time.deltaTime;
            if (tNetworkStatsDelay >= delay)
            {
                pingAmount++;
                using (System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping())
                {
                    pingSender.PingCompleted += (object sender, PingCompletedEventArgs e) =>
                    {
                        if (e.Reply.Status == IPStatus.Success)
                        {
                            ping = e.Reply.RoundtripTime;
                        }
                        else packetLoss += 1;
                    };
                    pingSender.SendAsync(IData.ipAddress, null);
                }
                tNetworkStatsDelay = 0;
            }
            Ping = ping;
            PcktLoss = (packetLoss / pingAmount) * 100;
        }

        public bool isLocalPlayer(Player mPlayer)
        {
            return mPlayer.Equals(MyPlayer);
        }

        private void ProcessClientData(byte[] buffer)
        {
            try
            {
                using (NeutronReader mReader = new NeutronReader(buffer))
                {
                    Packet mCommand = mReader.ReadPacket<Packet>();
                    switch (mCommand)
                    {
                        case Packet.Connected:
                            {
                                int port = mReader.ReadInt32();
                                byte[] array = mReader.ReadExactly();
                                ///////////////////////////////////////////////////////////////////////
                                endPointUDP = new IPEndPoint(IPAddress.Parse(IData.ipAddress), port);
                                ///////////////////////////////////////////////////////////////////////
                                HandleConnected(array);
                                new Action(() => OnNeutronConnected?.Invoke(true, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.DisconnectedByReason:
                            {
                                string reason = mReader.ReadString();
                                new Action(() => OnNeutronDisconnected?.Invoke(reason, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.SendChat:
                            {
                                string message = mReader.ReadString();
                                byte[] array = mReader.ReadExactly();
                                Player mSender = array.DeserializeObject<Player>();
                                new Action(() => OnMessageReceived?.Invoke(message, mSender, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.RPC:
                            {
                                int ID = mReader.ReadInt32();
                                byte[] array = mReader.ReadExactly();
                                HandleRPC(ID, array);
                            }
                            break;
                        case Packet.APC:
                            {
                                int ID = mReader.ReadInt32();
                                int playerID = mReader.ReadInt32();
                                byte[] Parameters = mReader.ReadExactly();
                                HandleAPC(ID, Parameters, playerID);
                            }
                            break;
                        case Packet.Static:
                            {
                                int ID = mReader.ReadInt32();
                                byte[] _array = mReader.ReadExactly();
                                byte[] array_ = mReader.ReadExactly();
                                Player mSender = _array.DeserializeObject<Player>();
                                HandleRCC(ID, mSender, array_, false);
                            }
                            break;
                        case Packet.Response:
                            {
                                int ID = mReader.ReadInt32();
                                byte[] parameters = mReader.ReadExactly();
                                HandleACC(ID, parameters);
                            }
                            break;
                        case Packet.GetChannels:
                            {
                                Channel[] channels = mReader.ReadExactly().DeserializeObject<Channel[]>();
                                new Action(() => OnChannelsReceived?.Invoke(channels, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.JoinChannel:
                            {
                                Player mSender = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerJoinedChannel?.Invoke(mSender, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.LeaveChannel:
                            {
                                Player mSender = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerLeftChannel?.Invoke(mSender)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.Fail:
                            {
                                Packet failedPacket = mReader.ReadPacket<Packet>();
                                string failMessage = mReader.ReadString();
                                new Action(() => OnFailed?.Invoke(failedPacket, failMessage, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.CreateRoom:
                            Room room = mReader.ReadExactly().DeserializeObject<Room>();
                            new Action(() => OnCreatedRoom?.Invoke(room, this)).ExecuteOnMainThread(this);
                            break;
                        case Packet.GetRooms:
                            {
                                Room[] rooms = mReader.ReadExactly().DeserializeObject<Room[]>();
                                new Action(() => OnRoomsReceived?.Invoke(rooms, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.JoinRoom:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerJoinedRoom?.Invoke(player, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.LeaveRoom:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerLeftRoom?.Invoke(player, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.DestroyPlayer:
                            new Action(() => OnPlayerDestroyed?.Invoke(this)).ExecuteOnMainThread(this);
                            break;
                        case Packet.Disconnected:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                HandlePlayerDisconnected(player);
                            }
                            break;
                        case Packet.SyncBehaviour:
                            {
                                int ID = mReader.ReadInt32();
                                string props = mReader.ReadString();
                                HandleJsonProperties(ID, props);
                            }
                            break;
                        case Packet.Nickname:
                            new Action(() => OnNicknameChanged?.Invoke(this)).ExecuteOnMainThread(this);
                            break;
                        case Packet.SetPlayerProperties:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerPropertiesChanged?.Invoke(player, this)).ExecuteOnMainThread(this);
                            }
                            break;
                        case Packet.SetRoomProperties:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnRoomPropertiesChanged?.Invoke(player, this)).ExecuteOnMainThread(this);
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) { Utils.StackTrace(ex); }
        }

        //public static void SendVoice (byte[] buffer, int lastPos) {
        //    using (NeutronWriter writer = new NeutronWriter ()) {
        //        writer.WritePacket (Packet.VoiceChat);
        //        writer.Write (lastPos);
        //        writer.Write (buffer);
        //        Send (writer.ToArray(), Protocol.Udp);
        //    }
        //}

        public void LeaveRoom()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.LeaveRoom);
                Send(writer.ToArray());
            }
        }

        public void LeaveChannel()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.LeaveChannel);
                Send(writer.ToArray());
            }
        }

        public void SendChat(string mMessage, Broadcast broadcast)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.SendChat);
                writer.WritePacket(broadcast);
                writer.Write(mMessage);
                Send(writer.ToArray());
            }
        }

        private void SetNickname(string nickname)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Nickname);
                writer.Write(nickname);
                Send(writer.ToArray());
            }
            _nickname = nickname;
        }

        public void JoinChannel(int ChannelID)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.JoinChannel);
                writer.Write(ChannelID);
                Send(writer.ToArray());
            }
        }

        public void JoinRoom(int RoomID)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.JoinRoom);
                writer.Write(RoomID);
                Send(writer.ToArray());
            }
        }

        public void CreateRoom(string roomName, int maxPlayers, string password, Dictionary<string, object> properties, bool visible = true, bool JoinOrCreate = false)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.CreateRoom);
                writer.Write(roomName);
                writer.Write(maxPlayers);
                writer.Write(password ?? string.Empty);
                writer.Write(visible);
                writer.Write(JoinOrCreate);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer.ToArray());
            }
        }

        public void GetCachedPackets(CachedPacket packet, int ID)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.GetChached);
                writer.WritePacket(packet);
                writer.Write(ID);
                Send(writer.ToArray());
            }
        }

        public void GetChannels()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.GetChannels);
                Send(writer.ToArray());
            }
        }

        public void GetRooms()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.GetRooms);
                Send(writer.ToArray());
            }
        }

        public void SetPlayerProperties(Dictionary<string, object> properties)
        {
            string _properties = JsonConvert.SerializeObject(properties);
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.Write(_properties);
                Send(writer.ToArray());
            }
        }

        public void SetRoomProperties(Dictionary<string, object> properties)
        {
            string _properties = JsonConvert.SerializeObject(properties);
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.SetRoomProperties);
                writer.Write(_properties);
                Send(writer.ToArray());
            }
        }

        /// <summary>
        /// Static: Creates an instance of this player on the server.
        /// <para>Exclusive Static ID: 1001</para> 
        /// </summary>
        public void CreatePlayer(MonoBehaviour mThis, NeutronWriter options, SendTo sendTo, Broadcast broadcast)
        {
            Static(mThis, 1001, options, true, sendTo, Protocol.Tcp, broadcast);
        }

        public void DestroyPlayer()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.DestroyPlayer);
                Send(writer.ToArray());
            }
        }

        public void Static(MonoBehaviour mThis, int RCCID, NeutronWriter writer, bool enableCache, SendTo sendTo, Protocol protocolType, Broadcast broadcast)
        {
            InternalRCC(mThis, RCCID, writer.ToArray(), enableCache, sendTo, protocolType, broadcast);
        }

        private bool RPCTimer(int RPCID, float Delay)
        {
            if (timeRPC.ContainsKey(RPCID))
            {
                timeRPC[RPCID] += Time.deltaTime;
                if (timeRPC[RPCID] >= Delay)
                {
                    timeRPC[RPCID] = 0;
                    return true;
                }
                return false;
            }
            else
            {
                timeRPC.Add(RPCID, 0);
                return true;
            }
        }

        public void RPC(int RPCID, float secondsDelay, NeutronWriter parametersStream, SendTo sendTo, bool enableCache, Broadcast broadcast, Protocol protocolType = Protocol.Tcp)
        {
            if (RPCTimer(RPCID, secondsDelay))
            {
                InternalRPC(MyPlayer.ID, RPCID, new object[] { parametersStream.ToArray() }, sendTo, enableCache, protocolType, broadcast);
            }
        }

        public void RPC(Player playerToSend, int RPCID, float secondsDelay, NeutronWriter parametersStream, SendTo sendTo, bool enableCache, Broadcast broadcast, Protocol protocolType = Protocol.Tcp)
        {
            if (RPCTimer(RPCID, secondsDelay))
            {
                InternalRPC(playerToSend.ID, RPCID, new object[] { parametersStream.ToArray() }, sendTo, enableCache, protocolType, broadcast);
            }
        }

        /// <summary>
        /// Notifies you of removing an item from the interface.
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="mParent">Transform parent of interface objects</param>
        /// <param name="mArray">The object that will generate the objects in the interface</param>
        /// <param name="mCacheArray">The list that stores the interface objects generated by mArray</param>
        /// <param name="mDestroy">Indicates whether to remove or destroy the interface object</param>
        /// <returns></returns>
        public bool? NotifyDestroy<T>(Transform mParent, T[] mArray, ref List<T> mCacheArray, IEqualityComparer<T> comparer, bool mDestroy = true) where T : INotify
        {
            if (mParent.childCount > mArray.Length)
            {
                var exceptList = mCacheArray.Except(mArray, comparer);
                int indexRemove = 0;
                foreach (var _ in exceptList.ToList())
                {
                    indexRemove = mCacheArray.RemoveAll(x => x.ID == _.ID);
                    foreach (Transform _child in mParent)
                    {
                        int ID = int.Parse(new string(_child.name.Where(x => char.IsNumber(x)).ToArray()));
                        if (ID == _.ID)
                        {
                            if (mDestroy) Destroy(_child.gameObject);
                            else _child.gameObject.SetActive(false);
                        }
                    }
                    //if (onNotify) onNotifyDestroy(_.ID, ((NOTIFY)new System.Diagnostics.StackFrame(1).GetMethod().GetCustomAttributes(typeof(NOTIFY), false).FirstOrDefault()).eventName);
                }
                //===========================================================================================
                return Convert.ToBoolean(indexRemove);
            }
            return null;
        }
        /// <summary>
        /// Indicates whether to remove or destroy the interface object
        /// </summary>
        /// <typeparam name="T">Generic Type</typeparam>
        /// <param name="mArray">The list that generated the objects in the interface</param>
        /// <param name="mObject">The expression that will be used to identify whether the objects are the same or not; x => x.ID == ..........</param>
        /// <returns></returns>
        public bool NotifyExist<T>(List<T> mArray, Func<T, bool> mObject)
        {
            return mArray.Any(mObject);
        }

        public void NotifyClear<T>(Transform mParent, List<T> mArray, bool destroy = true)
        {
            foreach (Transform _p in mParent)
            {
                if (destroy) Destroy(_p.gameObject);
                else _p.gameObject.SetActive(false);
                mArray.Clear();
            }
        }

        public void NotifyUpdate<T>(T notify, Transform mParent, Action<Transform, T> method) where T : INotify
        {
            foreach (Transform child in mParent.transform)
            {
                if (child.name == notify.ID.ToString())
                {
                    method.Invoke(child, notify);
                }
                else continue;
            }
        }

        public bool IsDuplicate(Player player)
        {
            return playersObjects.TryGetValue(player.ID, out NeutronView _);
        }

        public static Neutron CreateClient(ClientType type, GameObject ownerGameObject)
        {
            Neutron neutronInstance = ownerGameObject.AddComponent<Neutron>();
            neutronInstance.InitializeClient(type);
            if (type == ClientType.MainPlayer) Client = neutronInstance;
            return neutronInstance;
        }
    }
}