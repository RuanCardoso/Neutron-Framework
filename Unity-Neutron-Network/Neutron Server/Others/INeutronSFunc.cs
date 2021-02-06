using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using UnityEngine;
using System.Threading;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork;
using NeutronNetwork.Internal.Server.InternalEvents;

namespace NeutronNetwork.Internal.Server
{
    public class NeutronSFunc : NeutronSConst
    {
        public static NeutronSFunc _;

        public static event SEvents.OnPlayerDisconnected onPlayerDisconnected;
        public static event SEvents.OnPlayerDestroyed onPlayerDestroyed;
        public static event SEvents.OnPlayerJoinedChannel onPlayerJoinedChannel;
        public static event SEvents.OnPlayerLeaveChannel onPlayerLeaveChannel;
        public static event SEvents.OnPlayerJoinedRoom onPlayerJoinedRoom;
        public static event SEvents.OnPlayerLeaveRoom onPlayerLeaveRoom;
        public static SEvents.OnPlayerInstantiated onPlayerInstantiated;
        public static SEvents.OnPlayerPropertiesChanged onChanged;
        public static SEvents.OnCheatDetected onCheatDetected;
        public static SEvents.OnServerStart onServerStart;

        // MonoBheaviour

        public void Awake()
        {
            _ = this; // set this instance.
        }

        private void Update()
        {
            Utils.Dequeue(ref mainThreadActions, DPF); // process de server data.
        }

        // Server Functions

        public bool GetPlayer(TcpClient mSocket, out Player mSender)
        {
            bool _player = Players.TryGetValue(mSocket, out Player value); // thread safe? I have my doubts.
            mSender = value;
            return _player;
        }

        public bool GetPlayer(IPEndPoint pEndPoint, out Player mSender)
        {
            mSender = Players.Values.FirstOrDefault(x => x.tcpClient.RemoteEndPoint().Equals(pEndPoint)); // thread safe
            return !mSender.Equals(null);
        }

        public bool GetPlayer(int ID, out Player mSender)
        {
            mSender = Players.Values.FirstOrDefault(x => x.ID == ID);
            return !mSender.Equals(null);
        }

        protected bool AddPlayer(Player mPlayer)
        {
            return Players.TryAdd(mPlayer.tcpClient, mPlayer);
        }

        protected bool RemovePlayer(Player mPlayer)
        {
            bool removed = false;
            if (removed = Players.TryRemove(mPlayer.tcpClient, out Player removedPlayer)) // thread safe
            {
                // Actions are thread-safe.
                new Action(() =>
                {
                    if (removedPlayer.neutronView != null)
                        Destroy(removedPlayer.neutronView.gameObject); // destroy player object.
                }).ExecuteOnMainThread();
                return removed;
            }
            else return removed;
        }

        protected void DisposeAllClients()
        {
            Players.ToList().ForEach(x => x.Value?.Dispose());
        }

        protected void Dispose()
        {
            _TCPListen.Server.Close();
        }

        private void Cache(int ID, Player owner, byte[] buffer, CachedPacket packet)
        {
            CachedBuffer keyBuffer = new CachedBuffer();
            keyBuffer.ID = ID;
            keyBuffer.owner = owner;
            keyBuffer.buffer = buffer;
            keyBuffer.cachedPacket = packet;

            //Channel indexOf = Channels.FirstOrDefault(x => x.ID == owner.currentChannel);
            //int index = Channels.IndexOf(indexOf);
            //var cBuffers = Channels[index]._cachedPackets;
            //cBuffers.Add(keyBuffer);
        }

        public static void SendProperties(Player player, NeutronSyncBehaviour properties, SendTo sendTo, Broadcast broadcast)
        {
            //NeutronSyncBehaviour _properties = properties;
            ////------------------------------------------------------------
            //string props = JsonUtility.ToJson(_properties);
            ////------------------------------------------------------------
            //using (NeutronWriter writer = new NeutronWriter())
            //{
            //    writer.WritePacket(Packet.SyncBehaviour);
            //    writer.Write(player.ID);
            //    writer.Write(props);
            //    player.Send(sendTo, writer.ToArray(), broadcast, null, Protocol.Tcp);
            //}
        }

        public static void SendErrorMessage(Player mSocket, Packet packet, string message)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                mSocket.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
            }
        }

        public static void SendDisconnect(Player mSocket, string reason)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.DisconnectedByReason);
                writer.Write(reason);
                mSocket.Send(writer.ToArray());
            }
        }

        private void SendDisconnect(Player mSender)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.Disconnected);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(SendTo.Others, writer.ToArray(), Broadcast.Channel, Protocol.Tcp);
            }
        }
        /// <summary>
        /// Send the response from client or all clients.
        /// </summary>
        /// <param name="mSocket">TcpClient of player</param>
        /// <param name="buffer">Message stream</param>
        public static void TCP(TcpClient mSocket, SendTo sendTo, byte[] buffer, Player[] ToSend)
        {
            switch (sendTo)
            {
                case SendTo.Only:
                    _.Players[mSocket].qDataTCP.Enqueue(buffer);
                    break;
                case SendTo.All:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        ToSend[i].qDataTCP.Enqueue(buffer);
                    }
                    break;
                case SendTo.Others:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (ToSend[i].tcpClient == mSocket) continue;
                        ToSend[i].qDataTCP.Enqueue(buffer);
                    }
                    break;
            }
        }
        public static void UDP(TcpClient mSocket, SendTo sendTo, byte[] buffer, Player[] ToSend)
        {

            switch (sendTo)
            {
                case SendTo.Only:
                    _.Players[mSocket].qDataUDP.Enqueue(buffer);
                    break;
                case SendTo.All:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        ToSend[i].qDataUDP.Enqueue(buffer);
                    }
                    break;
                case SendTo.Others:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (ToSend[i].tcpClient == mSocket) continue;
                        ToSend[i].qDataUDP.Enqueue(buffer);
                    }
                    break;
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Handles
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void HandleDisconnect(Player disconnectedPlayer, CancellationTokenSource cts)
        {
            using (disconnectedPlayer) // release resources
            using (cts)
            {
                cts.Cancel(); // stop thread
                if (RemovePlayer(disconnectedPlayer)) // remove player from server. [Thread-Safe] 
                    onPlayerDisconnected?.Invoke(disconnectedPlayer); // Thread safe. delegate are immutable.
            }
        }

        protected void HandleConfirmation(Player mSender, bool isBot)
        {
            mSender.isBot = isBot; // [Thread-Safe - individual access, only the parent thread has access.]
            using (NeutronWriter writer = new NeutronWriter()) // write the message.
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.Connected); // packet name
                writer.Write(mSender.lPEndPoint.Port); // local udp port in server.
                writer.Write(arrayBytes.Length); // length of arrayBytes.
                writer.Write(arrayBytes); // array
                mSender.Send(writer.ToArray()); // send the message. [Thread-Safe]
            }
        }

        protected void HandleNickname(Player mSender, string Nickname)
        {
            mSender.Nickname = Nickname; // [Thread-Safe - individual access, only the parent thread has access.]
            using (NeutronWriter writer = new NeutronWriter()) // write the message.
            {
                writer.WritePacket(Packet.Nickname); // packet name
                mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp); // send the message. [Thread-Safe]
            }
        }

        protected void HandleSendChat(Player mSender, Broadcast broadcast, string message)
        {
            using (NeutronWriter writer = new NeutronWriter()) // write the message.
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.SendChat); // packet name
                writer.Write(message);
                writer.Write(arrayBytes.Length);
                writer.Write(arrayBytes);
                mSender.Send(SendTo.All, writer.ToArray(), broadcast, Protocol.Tcp); // send the message. [Thread-Safe]
            }
        }

        protected void HandleRPC(Player mSender, Broadcast broadcast, SendTo sendMode, int executeID, bool cacheEnabled, byte[] parameters, bool isUDP = false)
        {
            void Send()
            {
                using (NeutronWriter writer = new NeutronWriter()) // write the message.
                {
                    writer.WritePacket(Packet.RPC); // packet name
                    writer.Write(executeID); // write RPC ID.
                    writer.Write(parameters.Length); // write rpc length.
                    writer.Write(parameters); // write RPC.

                    Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
                    mSender.Send(sendMode, writer.ToArray(), broadcast, protocol); // send the message. [Thread-Safe]

                    if (cacheEnabled) Cache(executeID, mSender, writer.ToArray(), CachedPacket.RPC);
                }
            }

            if (mSender.IsInChannel()) // [Thread-Safe - individual access, only the parent thread has access.]
            {
                object[] _array = parameters.DeserializeObject<object[]>();
                int senderID = (int)_array[0];
                object[] objectParams = (object[])_array[1];

                new Action(() =>
                {
                    if (mSender.neutronView == null) Send();
                    else
                    {
                        if (Communication.InitRPC(executeID, objectParams, true, mSender.neutronView))
                        {
                            Send();
                        }
                    }
                }).ExecuteOnMainThread();
            }
            else SendErrorMessage(mSender, Packet.SendChat, "ERROR: You are not on a channel/room.");
        }

        protected void HandleRCC(Player mSender, Broadcast broadcast, SendTo sendMode, int executeID, bool cacheEnabled, byte[] parameters, bool isUDP = false)
        {
            new Action(() =>
            {
                if (Communication.InitRCC(executeID, mSender, parameters, true, null))
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = mSender.Serialize();

                        writer.WritePacket(Packet.Static); // packet name
                        writer.Write(executeID);
                        writer.Write(array.Length);
                        writer.Write(array);
                        writer.Write(parameters.Length);
                        writer.Write(parameters);

                        Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
                        mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);

                        if (cacheEnabled) Cache(executeID, mSender, writer.ToArray(), CachedPacket.Static);
                    }
                }
            }).ExecuteOnMainThread();
        }

        protected void HandleGetChannels(Player mSender, Packet mCommand)
        {
            try
            {
                if (!mSender.IsInChannel()) // Thread safe.
                {
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = Channels.Values.ToArray().Serialize();
                        writer.WritePacket(mCommand);
                        writer.Write(array.Length);
                        writer.Write(array);
                        mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You are trying to get channels from within a channel, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of channels within a channel, in order to save bandwidth.");
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }

        protected void HandleJoinChannel(Player mSender, Packet mCommand, int channelID)
        {
            // Channels is Thread-Safe because is a ConcurrentDictionary.
            try
            {
                if (Channels.Count == 0) // Thread-Safe - check if count > 0
                {
                    SendErrorMessage(mSender, mCommand, "ERROR: There are no channels created on the server.");
                    return;
                }

                if (Channels.TryGetValue(channelID, out Channel channel)) // Thread-Safe - get channel of ID
                {
                    if (!mSender.IsInChannel())
                    {
                        if (channel.AddPlayer(mSender, out string errorMessage))
                        {
                            mSender.currentChannel = channelID; // Thread safe - because mSender is individual (not simultaneous) - update current channel of player.
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = mSender.Serialize();
                                writer.WritePacket(mCommand); // write packet name.
                                writer.Write(array.Length);
                                writer.Write(array); // writes the player who sent/joined.
                                mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Channel, Protocol.Tcp); // Send request to player.
                            }
                            onPlayerJoinedChannel?.Invoke(mSender); // Thread safe - delegates are immutable. 
                        }
                        else SendErrorMessage(mSender, mCommand, errorMessage);
                    }
                    else SendErrorMessage(mSender, mCommand, "ERROR: You are already joined to a channel.");
                }
                else SendErrorMessage(mSender, mCommand, "ERROR: We couldn't find a channel with this ID.");

            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }
        protected void HandleCreateRoom(Player mSender, Packet mCommand, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
        {
            //try {
            //    if (mSender.IsInChannel () && !mSender.IsInRoom ()) {
            //        Room nRoom = new Room (roomID, roomName, maxPlayers, !string.IsNullOrEmpty (Password), isVisible, options);
            //        //---------------------------------------------------------------------------------------------------------
            //        void CreateRoom () {
            //            //-----------------------------------------------------------------------------------------------------
            //            int index = Channels.FindIndex (x => x.ID == mSender.currentChannel);
            //            Channels[index]._rooms.Add (nRoom);
            //            //-----------------------------------------------------------------------------------------------------
            //            mSender.currentRoom = roomID;
            //            tcpPlayers[mSender.tcpClient] = mSender;
            //            //-----------------------------------------------------------------------------------------------------
            //            roomID++;
            //            //-----------------------------------------------------------------------------------------------------
            //            using (NeutronWriter writer = new NeutronWriter ()) {
            //                writer.WritePacket (mCommand);
            //                writer.Write (nRoom.Serialize ());
            //                mSender.Send (SendTo.Only, writer.ToArray(), Broadcast.None, null, Protocol.Tcp);
            //            }
            //        }
            //        if (!JoinOrCreate) {
            //            CreateRoom ();
            //        } else {
            //            if (!Channels[Channels.FindIndex (x => x.ID == mSender.currentChannel)]._rooms.Any (x => x.Name == roomName)) {
            //                CreateRoom ();
            //            } else HandleJoinRoom (mSender, Packet.JoinRoom, roomID);
            //        }
            //    } else SendErrorMessage (mSender, mCommand, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            //} catch (Exception ex) { Utils.LoggerError (ex.Message); }
        }

        protected void HandleGetCached(Player mSender, CachedPacket packetToSendCache, int ID)
        {
            //Channel indexOf = Channels.FirstOrDefault(x => x.ID == mSender.currentChannel);
            ////------------------------------------------------------------------------------------
            //int index = Channels.IndexOf(indexOf);
            ////------------------------------------------------------------------------------------
            //var cBuffers = Channels[index]._cachedPackets;
            ////------------------------------------------------------------------------------------
            //foreach (var cached in cBuffers.ToList())
            //{
            //    CachedBuffer keyBuffer = cached;

            //    if (keyBuffer.owner.Equals(mSender)) continue;

            //    if (ID > -1)
            //    {
            //        if (keyBuffer.cachedPacket == packetToSendCache && keyBuffer.ID == ID)
            //        {
            //            using (NeutronWriter writer = new NeutronWriter())
            //            {
            //                writer.WritePacket(Packet.GetChached);
            //                writer.Write(keyBuffer.owner.ID);
            //                writer.Write(keyBuffer.owner.GetSViewer().lastPosition);
            //                writer.Write(keyBuffer.owner.GetSViewer().lastRotation);
            //                writer.Write(keyBuffer.buffer);
            //                if (ID == 1001) mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, null, Protocol.Tcp);
            //                else mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.None, null, Protocol.Tcp);
            //            }
            //        }
            //        else continue;
            //    }
            //    else if (ID == -1)
            //    {
            //        if (keyBuffer.cachedPacket == packetToSendCache)
            //        {
            //            mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.None, null, Protocol.Tcp);
            //        }
            //        else continue;
            //    }
            //}
        }

        protected void HandleGetRooms(Player mSender, Packet mCommand)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = Channels[mSender.currentChannel];
                    //if (Channels[indexChannel]._rooms.Count == 0) return;
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] array = channel.GetRooms().Serialize();
                        writer.WritePacket(mCommand);
                        writer.Write(array.Length);
                        writer.Write(array);
                        mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }

        protected void HandleLeaveRoom(Player mSender, Packet mCommand)
        {
            if (mSender.IsInRoom())
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();
                    writer.WritePacket(mCommand);
                    writer.Write(array.Length);
                    writer.Write(array);
                    mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Room, Protocol.Tcp);
                }
                mSender.currentRoom = -1;
            }
            else SendErrorMessage(mSender, mCommand, "ERROR: LeaveRoom Failed");
        }

        protected void HandleLeaveChannel(Player mSender, Packet mCommand)
        {
            if (mSender.IsInChannel())
            {
                using (NeutronWriter writer = new NeutronWriter())
                {
                    byte[] array = mSender.Serialize();
                    writer.WritePacket(mCommand);
                    writer.Write(array.Length);
                    writer.Write(array);
                    mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Channel, Protocol.Tcp);
                }
                mSender.currentChannel = -1;
            }
            else SendErrorMessage(mSender, mCommand, "ERROR: LeaveChannel Failed");
        }

        protected void HandleJoinRoom(Player mSender, Packet mCommand, int roomID)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = Channels[mSender.currentChannel]; // Thread safe
                    Room room = channel.GetRoom(roomID); // thread safe

                    if (room == null) return;

                    if (room.AddPlayer(mSender, out string errorMessage))
                    {
                        mSender.currentRoom = roomID;
                        using (NeutronWriter writer = new NeutronWriter())
                        {
                            byte[] array = mSender.Serialize();
                            writer.WritePacket(mCommand);
                            writer.Write(array.Length);
                            writer.Write(array);
                            mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Room, Protocol.Tcp);
                        }
                        onPlayerJoinedRoom?.Invoke(mSender);
                    }
                    else SendErrorMessage(mSender, mCommand, errorMessage);
                }
                else SendErrorMessage(mSender, mCommand, "WARNING: You already in channel?/or/You are trying to get rooms from within a room, this function is not necessarily prohibited, you can change the behavior on the server, but it is not recommended to obtain the list of rooms within a room, in order to save bandwidth.");
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }
        protected void HandleDestroyPlayer(Player mSender, Packet mCommand)
        {
            NeutronView obj = mSender.neutronView;
            if (obj == null) return;
            new Action(() =>
            {
                Destroy(obj.gameObject);
                using (NeutronWriter writer = new NeutronWriter())
                {
                    writer.WritePacket(mCommand);
                    mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                }
                onPlayerDestroyed?.Invoke(mSender);
            }).ExecuteOnMainThread();
        }
        protected void HandleServerObject(Player mSender, Broadcast broadcast, string prefabName, Vector3 pos, Quaternion rot, bool sendtoOthersPlayers, Identity identity)
        {
            //new Action (() => {
            //    GameObject playerPref = Resources.Load (prefabName, typeof (GameObject)) as GameObject;
            //    if (playerPref != null) {
            //        GameObject obj = Instantiate (playerPref, pos, rot);
            //        //------------------------------------------------------------------
            //        obj.name += $" -> [SERVER OBJECT]";
            //        //------------------------------------------------------------------
            //        obj.GetComponent<NeutronIdentity> ().Identity = identity;
            //        //------------------------------------------------------------------
            //        int layerMask = LayerMask.NameToLayer ("ServerObject");
            //        //------------------------------------------------------------------
            //        if (layerMask > -1) obj.layer = layerMask;
            //        //------------------------------------------------------------------
            //        if (sendtoOthersPlayers) {
            //            using (NeutronWriter writer = new NeutronWriter ()) {
            //                writer.WritePacket (Packet.ServerObjectInstantiate);
            //                writer.Write (prefabName);
            //                writer.Write (pos);
            //                writer.Write (rot);
            //                writer.Write (identity.Serialize ());
            //                //---------------------------------------------------------------------------------------------------------------
            //                mSender.Send (SendTo.Others, writer.ToArray(), broadcast, null, Protocol.Tcp);
            //            }
            //        }
            //        //------------------------------------------------------------------
            //        Destroy (obj, 15f);
            //    } else Utils.LoggerError ($"Unable to load {prefabName} check if this prefab exist in resources folder");
            //}).ExecuteOnMainThread ();
        }
    }
}