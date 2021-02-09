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
using System.IO;

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

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //MonoBehaviour
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public void Awake()
        {
            _ = this; // set this instance.
        }

        private void Update()
        {
            Utils.Dequeue(ref mainThreadActions, DPF); // process de server data. // [Thread-Safe]
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Server Functions
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        public bool GetPlayer(TcpClient mSocket, out Player mSender) // [Thread-Safe]
        {
            mSender = Players[mSocket];
            return mSender != null;
        }

        public bool GetPlayer(IPEndPoint pEndPoint, out Player mSender) // [Thread-Safe]
        {
            mSender = Players.Values.FirstOrDefault(x => x.tcpClient.RemoteEndPoint().Equals(pEndPoint));
            return mSender != null;
        }

        public bool GetPlayer(int ID, out Player mSender) // [Thread-Safe]
        {
            mSender = Players.Values.FirstOrDefault(x => x.ID == ID);
            return mSender != null;
        }

        protected bool AddPlayer(Player mPlayer) // [Thread-Safe]
        {
            return Players.TryAdd(mPlayer.tcpClient, mPlayer);
        }

        protected bool RemovePlayer(Player mPlayer) // [Thread-Safe]
        {
            if (Players.TryRemove(mPlayer.tcpClient, out Player removedPlayer))
            {
                if (removedPlayer.neutronView != null)
                {
                    new Action(() =>
                    {
                        Destroy(removedPlayer.neutronView.gameObject); // destroy player object.
                    }).ExecuteOnMainThread();
                }
                return true;
            }
            else return false;
        }

        protected void DisposeAllClients() // [Thread-Safe]
        {
            Players.ToList().ForEach(x => x.Value?.Dispose());
        }

        protected void Dispose() // [Thread-Safe]
        {
            _TCPListen.Server.Close();
        }

        private void Cache(int ID, Player owner, byte[] buffer, CachedPacket packet) // [Thread-Safe]
        {
            CachedBuffer keyBuffer = new CachedBuffer();
            keyBuffer.ID = ID;
            keyBuffer.owner = owner;
            keyBuffer.buffer = buffer;
            keyBuffer.cachedPacket = packet;

            Channel channel = Channels[owner.currentChannel];
            channel.AddCache(keyBuffer);
        }

        public static void SendErrorMessage(Player mSocket, Packet packet, string message) // [Thread-Safe]
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                mSocket.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
            }
        }

        public static void SendDisconnect(Player mSocket, string reason) // [Thread-Safe]
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.DisconnectedByReason);
                writer.Write(reason);
                mSocket.Send(writer.ToArray());
            }
        }

        private void SendDisconnect(Player mSender) // [Thread-Safe]
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
        /// <param name="mPlayer">TcpClient of player</param>
        /// <param name="buffer">Message stream</param>
        public static void SocketProtocol(Player mPlayer, SendTo sendTo, byte[] buffer, Player[] ToSend, bool isUDP) // [Thread-Safe]
        {
            if (ToSend == null) sendTo = SendTo.Only;
            switch (sendTo)
            {
                case SendTo.Only:
                    if (!isUDP)
                        mPlayer.qDataTCP.Enqueue(buffer);
                    else mPlayer.qDataUDP.Enqueue(buffer);
                    break;
                case SendTo.All:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (mPlayer.isBot)
                            if (!ToSend[i].Equals(mPlayer) && ToSend[i].isBot) continue;
                        if (!isUDP)
                            ToSend[i].qDataTCP.Enqueue(buffer);
                        else ToSend[i].qDataUDP.Enqueue(buffer);
                    }
                    break;
                case SendTo.Others:
                    for (int i = 0; i < ToSend.Length; i++)
                    {
                        if (ToSend[i].Equals(mPlayer)) continue;
                        else if (mPlayer.isBot && ToSend[i].isBot) continue;
                        if (!isUDP)
                            ToSend[i].qDataTCP.Enqueue(buffer);
                        else ToSend[i].qDataUDP.Enqueue(buffer);
                    }
                    break;
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Handles
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void HandleDisconnect(Player disconnectedPlayer, CancellationTokenSource cts) // [Thread-Safe]
        {
            using (disconnectedPlayer) // release resources
            using (cts)
            {
                cts.Cancel(); // stop thread
                if (RemovePlayer(disconnectedPlayer)) // remove player from server. [Thread-Safe] 
                    onPlayerDisconnected?.Invoke(disconnectedPlayer); // Thread safe. delegate are immutable.
            }
        }

        protected void HandleConfirmation(Player mSender, bool isBot) // [Thread-Safe]
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

        protected void HandleNickname(Player mSender, string Nickname) // [Thread-Safe]
        {
            mSender.Nickname = Nickname; // [Thread-Safe - individual access, only the parent thread has access.]
            using (NeutronWriter writer = new NeutronWriter()) // write the message.
            {
                writer.WritePacket(Packet.Nickname); // packet name
                mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp); // send the message. [Thread-Safe]
            }
        }

        protected void HandleSendChat(Player mSender, Broadcast broadcast, string message) // [Thread-Safe]
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
        // [Thread-Safe]
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
                    if (GetPlayer(senderID, out Player findPlayer))
                    {
                        if (findPlayer.neutronView == null) Send();
                        else
                        {
                            if (Communication.InitRPC(executeID, objectParams, findPlayer.neutronView))
                            {
                                Send();
                            }
                        }
                    }
                }).ExecuteOnMainThread();
            }
            else SendErrorMessage(mSender, Packet.SendChat, "ERROR: You are not on a channel/room.");
        }
        // [Thread-Safe]
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
                        writer.WriteExactly(array);
                        writer.WriteExactly(parameters);

                        Protocol protocol = (isUDP) ? Protocol.Udp : Protocol.Tcp;
                        mSender.Send(sendMode, writer.ToArray(), broadcast, protocol);

                        if (cacheEnabled) Cache(executeID, mSender, writer.ToArray(), CachedPacket.Static);
                    }
                }
            }).ExecuteOnMainThread();
        }
        // [Thread-Safe]
        protected void HandleGetChannels(Player mSender, Packet mCommand)
        {
            try
            {
                Debug.Log(mSender.ID);
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
        // [Thread-Safe]
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
        // [Thread-Safe]
        protected void HandleCreateRoom(Player mSender, Packet mCommand, string roomName, int maxPlayers, string Password, bool isVisible, bool JoinOrCreate, string options)
        {
            try
            {
                if (mSender.IsInChannel() && !mSender.IsInRoom())
                {
                    Channel channel = Channels[mSender.currentChannel];
                    int automaticID = channel.CountOfRooms;
                    Room nRoom = new Room(automaticID, roomName, maxPlayers, !string.IsNullOrEmpty(Password), isVisible, options);

                    void CreateRoom()
                    {
                        if (channel.AddRoom(nRoom, out string errorMessage))
                        {
                            mSender.currentRoom = automaticID;
                            using (NeutronWriter writer = new NeutronWriter())
                            {
                                byte[] array = nRoom.Serialize();
                                writer.WritePacket(mCommand);
                                writer.WriteExactly(array);
                                mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                            }
                        }
                        else SendErrorMessage(mSender, mCommand, errorMessage);
                    }

                    if (!JoinOrCreate)
                    {
                        CreateRoom();
                    }
                    else
                    {
                        if (!Channels[mSender.currentChannel].RoomExists(roomName))
                        {
                            CreateRoom();
                        }
                        else HandleJoinRoom(mSender, Packet.JoinRoom, automaticID);
                    }
                }
                else SendErrorMessage(mSender, mCommand, "ERROR: You cannot create a room by being inside one. Call LeaveRoom or you not within a channel");
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }
        // [Thread-Safe]
        protected void HandleGetCached(Player mSender, CachedPacket packetToSendCache, int ID)
        {
            Channel channel = Channels[mSender.currentChannel];
            CachedBuffer[] buffers = channel.GetCaches();
            foreach (var cached in buffers)
            {
                CachedBuffer keyBuffer = cached;

                if (keyBuffer.owner.Equals(mSender)) continue;

                if (ID > -1)
                {
                    if (keyBuffer.cachedPacket == packetToSendCache && keyBuffer.ID == ID)
                    {
                        if (ID == 1001)
                        {
                            using (NeutronReader lastReader = new NeutronReader(keyBuffer.buffer))
                            {
                                Packet packet = lastReader.ReadPacket<Packet>();
                                int _ID = lastReader.ReadInt32();
                                byte[] sender = lastReader.ReadExactly();
                                byte[] parameters = lastReader.ReadExactly();

                                using (NeutronWriter oldWriter = new NeutronWriter(new MemoryStream(parameters)))
                                {
                                    NeutronView view = keyBuffer.owner.neutronView;
                                    if (view != null)
                                    {
                                        oldWriter.Write(view.lastPosition);
                                        oldWriter.Write(view.lastRotation);
                                        parameters = oldWriter.ToArray(); // modified.
                                        using (NeutronWriter writer = new NeutronWriter())
                                        {
                                            writer.WritePacket(packet); // packet name
                                            writer.Write(_ID);
                                            writer.WriteExactly(sender);
                                            writer.WriteExactly(parameters);
                                            mSender.Send(SendTo.Only, writer.ToArray(), Broadcast.None, Protocol.Tcp);
                                        }
                                    }
                                }
                            }
                        }
                        else mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.None, Protocol.Tcp);
                    }
                    else continue;
                }
                else if (ID == -1)
                {
                    if (keyBuffer.cachedPacket == packetToSendCache)
                    {
                        mSender.Send(SendTo.Only, keyBuffer.buffer, Broadcast.None, Protocol.Tcp);
                    }
                    else continue;
                }
            }
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

        public void HandleSetPlayerProperties(Player mSender, string properties) // [THREAD-SAFE]
        {
            mSender.SetProperties(properties); // [THREAD-SAFE]
            using (NeutronWriter writer = new NeutronWriter())
            {
                byte[] arrayBytes = mSender.Serialize(); // write the player.
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.WriteExactly(arrayBytes);
                mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Channel, Protocol.Tcp);
            }
        }

        public void HandleSetRoomProperties(Player mSender, string properties) // [THREAD-SAFE]
        {
            if (mSender.IsInRoom())
            {
                Channel channel = Channels[mSender.currentChannel];
                Room room = channel.GetRoom(mSender.currentRoom);

                if (room.Owner == null)
                {
                    SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
                    return;
                }

                if (room.Owner.Equals(mSender))
                {
                    room.SetProperties(properties);
                    using (NeutronWriter writer = new NeutronWriter())
                    {
                        byte[] arrayBytes = mSender.Serialize(); // write the player.
                        writer.WritePacket(Packet.SetRoomProperties);
                        writer.WriteExactly(arrayBytes);
                        mSender.Send(SendTo.All, writer.ToArray(), Broadcast.Room, Protocol.Tcp);
                    }
                }
                else SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not allowed to change the properties of this room.");
            }
            else SendErrorMessage(mSender, Packet.SetRoomProperties, "You are not inside a room.");
        }
    }
}