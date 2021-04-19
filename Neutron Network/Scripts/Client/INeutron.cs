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
using NeutronNetwork.Internal.Client.Delegates;
using NeutronNetwork.Internal.Attributes;
using System.IO;
using NeutronNetwork.Internal;
using System.Threading.Tasks;
using System.Collections;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CLIENT)]
    public class Neutron : NeutronClientFunctions
    {
        public const string CONTAINER_NAME = "[Container] -> Player[Main]";
        public const int GENERATE_PLAYER_ID = 27716848;
        /// <summary>
        /// Returns instance of server
        /// </summary>
        public static NeutronServer Server
        {
            get
            {
                if (NeutronServerFunctions._ != null) return NeutronServerFunctions._;
#if !UNITY_EDITOR && !UNITY_SERVER
                NeutronUtils.LoggerError("You cannot access the server's methods and properties on the client, except within the Unity Editor.");
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
        public bool IsReady { get; private set; }
        /// <summary>
        /// Get/Set field private.
        /// </summary>
        private string _nickname;
        /// <summary>
        /// The nickname of the player that will be used to be displayed to other players.
        /// </summary>
        public string Nickname
        {
            get => _nickname;
            set => SetNickname(value);
        }
        public Room currentRoom;
        public Channel currentChannel;
        /// <summary>
        /// Time of server;
        /// </summary>
        public double CurrentTime { get; set; }
        private double serverTime;
        private double TimeAsDouble;
        private double diff;
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
        /// This event is called when any player connection to the server fails.
        /// </summary>
        public event Events.OnPlayerDisconnected OnPlayerDisconnected;
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
        public event Events.OnPlayerNicknameChanged OnPlayerNicknameChanged;
        /// <summary>
        /// This event is triggered when your or other players change their properties.
        /// </summary>
        public event Events.OnPlayerPropertiesChanged OnPlayerPropertiesChanged;
        /// <summary>
        /// This event is triggered when room properties changed.
        /// </summary>
        public event Events.OnRoomPropertiesChanged OnRoomPropertiesChanged;

        bool SocketConnected(Socket s)
        {
            try
            {
                bool part1 = s.Poll(1000, SelectMode.SelectRead);
                bool part2 = (s.Available == 0);
                if (part1 && part2)
                    return false;
                else
                    return true;
            }
            catch { return false; }
        }

        private void Update()
        {
            TimeAsDouble = (double)Time.unscaledTime;
            if (!IsConnected) return;
#if !UNITY_SERVER
            if (Server == null)
                Application.targetFrameRate = NeutronConfig.Settings.ClientSettings.FPS;
#endif
            CurrentTime = (TimeAsDouble - diff) + serverTime;
            if (MyPlayer != null && IsReady)
                MyPlayer.infor = new NeutronMessageInfo(CurrentTime);
            //else CurrentTime = serverTime;

            // if (!SocketConnected(_TCPSocket.Client))
            // {
            //     Dispose();
            //     Destroy(this);
            // }
        }

        /// <summary>
        /// This function will trigger the OnPlayerConnected callback.
        /// Do not call this function in Awake.
        /// </summary>
        /// <param name="ipAddress">The ip of server to connect</param>
        public async void Connect()
        {
            await Task.Delay(5);
            try
            {
                if (!IsBot)
                {
#if UNITY_SERVER
            NeutronUtils.LoggerError($"MainClient disabled in server!\r\n");
            return;
#endif
                }
                else if (IsBot)
                {
#if !UNITY_SERVER && !UNITY_EDITOR
            NeutronUtils.LoggerError($"Bots disabled in client!");
            return;
#endif
                }
                Internal(); // initialize cliente.
                _TCPSocket.NoDelay = NeutronConfig.Settings.GlobalSettings.NoDelay;
                if (!NeutronUtils.LoggerError("Failed to load settings.", NeutronConfig.Settings)) return;
                //if (IData.ipAddress.Equals("LocalHost", StringComparison.InvariantCultureIgnoreCase) || IsBot) IData.ipAddress = "127.0.0.1";
                if (!IsConnected)
                {
                    IPAddress Ip = null;
                    string toReplace = NeutronConfig.Settings.GlobalSettings.Address;
                    toReplace = toReplace.Replace("http://", string.Empty);
                    toReplace = toReplace.Replace("https://", string.Empty);
                    toReplace = toReplace.Replace("/", string.Empty);
                    NeutronConfig.Settings.GlobalSettings.Address = toReplace;
                    if (IPAddress.TryParse(NeutronConfig.Settings.GlobalSettings.Address, out var ip))
                    {
                        Ip = ip;
                    }
                    else
                    {
                        Ip = (await Dns.GetHostAddressesAsync(NeutronConfig.Settings.GlobalSettings.Address))[0];
                        NeutronConfig.Settings.GlobalSettings.Address = Ip.ToString();
                    }
                    await _TCPSocket.ConnectAsync(Ip, NeutronConfig.Settings.GlobalSettings.Port); // await connection.
                    if (_TCPSocket.Connected)
                    {
                        IsConnected = true;
                        InitConnect();
                        if (!IsBot)
                        {
                            Thread _client = new Thread(() =>
                            {
                                ReadTCPData(_cts.Token);
                                ReadUDPData(_cts.Token);
                            });
                            _client.Priority = System.Threading.ThreadPriority.BelowNormal;
                            _client.IsBackground = true;
                            _client.Start();

                            StartCoroutine(Heartbeat());
                        }
                        else
                        {
                            ThreadPool.QueueUserWorkItem((e) =>
                            {
                                ReadTCPData(_cts.Token);
                                ReadUDPData(_cts.Token);
                            });
                        }
                    }
                    else if (!_TCPSocket.Connected)
                    {
                        IsConnected = false;
                        NeutronUtils.LoggerError("An attempt to establish a connection to the server failed.");
                        Dispose();
                        Destroy(this);
                        new Action(() => OnNeutronConnected?.Invoke(false, this)).DispatchOnMainThread();
                    }
                }
                else
                {
                    IsConnected = false;
                    NeutronUtils.LoggerError("Connection Refused!");
                    Dispose();
                    Destroy(this);
                }
            }
            catch
            {
                IsConnected = false;
                NeutronUtils.LoggerError("An attempt to establish a connection to the server failed.");
                Dispose();
                Destroy(this);
                new Action(() => OnNeutronConnected?.Invoke(false, this)).DispatchOnMainThread();
            }
        }

        private IEnumerator Heartbeat()
        {
            while (true)
            {
                if (IsConnected && IsReady && _UDPSocket.Client != null)
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        writer.WritePacket(Packet.Heartbeat);
                        writer.Write(CurrentTime);
                        Send(writer.ToArray(), Protocol.Udp);
                    }
                }
                yield return new WaitForSeconds(1f);
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
                    ProcessClientData(udpReceiveResult.Buffer);
                    InternalUtils.UpdateStatistics(Statistics.ClientRec, udpReceiveResult.Buffer.Length);
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
                        if (await Communication.ReadAsyncBytes(buffStream, messageLenBuffer, 0, sizeof(int), token))
                        {
                            int fixedLength = BitConverter.ToInt32(messageLenBuffer, 0);
                            byte[] messageBuffer = new byte[fixedLength];
                            if (await Communication.ReadAsyncBytes(buffStream, messageBuffer, 0, fixedLength, token))
                            {
                                using (NeutronReader messageReader = new NeutronReader(messageBuffer, 0, fixedLength))
                                {
                                    ProcessClientData(messageReader.ToArray());
                                    InternalUtils.UpdateStatistics(Statistics.ClientRec, fixedLength);
                                }
                            }
                        }
                        else
                        {
                            Dispose();
                            new Action(() => Destroy(this)).DispatchOnMainThread();
                        }
                    } while (!token.IsCancellationRequested);
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
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
                    pingSender.SendAsync(NeutronConfig.Settings.GlobalSettings.Address, null);
                }
                tNetworkStatsDelay = 0;
            }
            Ping = ping;
            PcktLoss = (packetLoss / pingAmount) * 100;
        }

        public bool IsMine(Player mPlayer)
        {
            return mPlayer.Equals(MyPlayer);
        }

        public bool IsMasterClient()
        {
            return currentRoom.Owner.Equals(MyPlayer);
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
                        case Packet.Heartbeat:
                            break;
                        case Packet.CustomTest:
                            NeutronUtils.LoggerError("Teste do servidor...... recebido");
                            break;
                        case Packet.Connected:
                            {
                                serverTime = mReader.ReadDouble();
                                int port = mReader.ReadInt32();
                                byte[] array = mReader.ReadExactly();
                                ///////////////////////////////////////////////////////////////////////
                                endPointUDP = new IPEndPoint(IPAddress.Parse(NeutronConfig.Settings.GlobalSettings.Address), port);
                                ///////////////////////////////////////////////////////////////////////
                                HandleConnected(array);
                                new Action(() => OnNeutronConnected?.Invoke(true, this)).DispatchOnMainThread();
                                diff = TimeAsDouble;
                                IsReady = true;
                            }
                            break;
                        case Packet.DisconnectedByReason:
                            {
                                string reason = mReader.ReadString();
                                new Action(() => OnNeutronDisconnected?.Invoke(reason, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.Chat:
                            {
                                string message = mReader.ReadString();
                                byte[] array = mReader.ReadExactly();
                                Player mSender = array.DeserializeObject<Player>();
                                new Action(() => OnMessageReceived?.Invoke(message, mSender, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.Dynamic:
                            {
                                int playerID = mReader.ReadInt32();
                                int rpcID = mReader.ReadInt32();
                                byte[] parameters = mReader.ReadExactly();
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                NeutronMessageInfo infor = mReader.ReadExactly().DeserializeObject<NeutronMessageInfo>();
                                HandleRPC(rpcID, playerID, parameters, player, infor);
                            }
                            break;
                        case Packet.NonDynamic:
                            {
                                int ID = mReader.ReadInt32();
                                byte[] _array = mReader.ReadExactly();
                                byte[] array_ = mReader.ReadExactly();
                                Player mSender = _array.DeserializeObject<Player>();
                                HandleRCC(ID, mSender, array_, false);
                            }
                            break;
                        case Packet.GetChannels:
                            {
                                Channel[] channels = mReader.ReadExactly().DeserializeObject<Channel[]>();
                                new Action(() => OnChannelsReceived?.Invoke(channels, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.JoinChannel:
                            {
                                Player mSender = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerJoinedChannel?.Invoke(mSender, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.LeaveChannel:
                            {
                                Player mSender = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerLeftChannel?.Invoke(mSender)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.Fail:
                            {
                                Packet failedPacket = mReader.ReadPacket<Packet>();
                                string failMessage = mReader.ReadString();
                                new Action(() => OnFailed?.Invoke(failedPacket, failMessage, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.CreateRoom:
                            {
                                Room room = mReader.ReadExactly().DeserializeObject<Room>();
                                new Action(() => OnCreatedRoom?.Invoke(room, this)).DispatchOnMainThread();
                                break;
                            }
                        case Packet.GetRooms:
                            {
                                Room[] rooms = mReader.ReadExactly().DeserializeObject<Room[]>();
                                new Action(() => OnRoomsReceived?.Invoke(rooms, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.JoinRoom:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerJoinedRoom?.Invoke(player, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.LeaveRoom:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerLeftRoom?.Invoke(player, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.DestroyPlayer:
                            new Action(() => OnPlayerDestroyed?.Invoke(this)).DispatchOnMainThread();
                            break;
                        case Packet.PlayerDisconnected:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerDisconnected?.Invoke(player, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.Nickname:
                            {
                                Player player = mReader.ReadExactly<Player>();
                                new Action(() => OnPlayerNicknameChanged?.Invoke(player, IsMine(player), this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.SetPlayerProperties:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnPlayerPropertiesChanged?.Invoke(player, this)).DispatchOnMainThread();
                            }
                            break;
                        case Packet.SetRoomProperties:
                            {
                                Player player = mReader.ReadExactly().DeserializeObject<Player>();
                                new Action(() => OnRoomPropertiesChanged?.Invoke(player, this)).DispatchOnMainThread();
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
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
                writer.WritePacket(Packet.Chat);
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

        public void GetCachedPackets(CachedPacket packet, int ID, bool includeMe = true)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.GetChached);
                writer.WritePacket(packet);
                writer.Write(ID);
                writer.Write(includeMe);
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
        public void CreatePlayer(MonoBehaviour mThis, NeutronWriter options)
        {
            NonDynamic(1001, options, Protocol.Tcp);
        }

        public void CreateObject(MonoBehaviour mThis, NeutronWriter options)
        {
            NonDynamic(1002, options, Protocol.Tcp);
        }

        public void DestroyPlayer()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.DestroyPlayer);
                Send(writer.ToArray());
            }
        }

        public void NonDynamic(int nID, NeutronWriter writer, Protocol protocolType)
        {
            InternalRCC(nID, writer.ToArray(), protocolType);
        }

        public void Dynamic(int nID, int dynamicID, NeutronWriter parametersStream, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            InternalRPC(nID, dynamicID, parametersStream.ToArray(), cacheMode, sendTo, broadcast, protocolType);
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
        public bool? NotifyDestroy<T>(Transform mParent, T[] mArray, ref List<T> mCacheArray, IEqualityComparer<T> comparer, bool mDestroy = true) where T : INeutronNotify
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

        public void NotifyUpdate<T>(T notify, Transform mParent, Action<Transform, T> method) where T : INeutronNotify
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
            return networkObjects.TryGetValue(player.ID, out NeutronView _);
        }

        public static Neutron CreateClient(ClientType type, GameObject ownerGameObject)
        {
            Neutron neutronInstance = ownerGameObject.AddComponent<Neutron>();
            neutronInstance.InitializeClient(type);
            if (type == ClientType.MainPlayer) Client = neutronInstance;
            return neutronInstance;
        }

        public static NeutronView Spawn(bool IsServer, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            NeutronView Spawn() => Instantiate(prefab, position, rotation).GetComponent<NeutronView>();
            if (prefab.TryGetComponent<NeutronView>(out NeutronView neutronView))
            {
                switch (neutronView.ambient)
                {
                    case Ambient.Both:
                        return Spawn();
                    case Ambient.Server:
                        if (IsServer)
                            return Spawn();
                        else return null;
                    case Ambient.Client:
                        if (!IsServer)
                            return Spawn();
                        else return null;
                    default:
                        return null;
                }
            }
            else return null;
        }

        public class UITools
        {

        }
    }
}