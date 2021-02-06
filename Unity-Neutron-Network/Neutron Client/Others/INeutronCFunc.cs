using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork.Internal.Client
{
    public class NeutronCFunc : NeutronCConst
    {
        /// <summary>
        /// Get instance of derived class.
        /// </summary>
        private Neutron _;
        /// <summary>
        /// defines is a bot or not.
        /// </summary>
        protected bool _isBot;
        /// <summary>
        /// Returns to the local player's instance.
        /// </summary>
        protected Player _myPlayer;
        /// <summary>
        /// Initializes the client and activates the response events.
        /// </summary>
        /// <param name="isBot">Tells whether the virtual player should behave like a bot.</param>
        protected void InitializeClient(ClientType type)
        {
            _ = (Neutron)this;
            _isBot = (type == ClientType.Bot);
            //--------------------------------------------------------------
            _.OnNeutronConnected += OnConnected;
            _.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            _.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            _.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            _.OnPlayerLeftChannel += OnPlayerLeftChannel;
            _.OnPlayerLeftRoom += OnPlayerLeftRoom;
            _.OnFailed += OnFailed;
            _.OnNeutronDisconnected += OnDisconnected;
            _.OnCreatedRoom += OnCreatedRoom;
        }

        protected void Send(byte[] buffer, Protocol protocolType = Protocol.Tcp)
        {
            buffer = buffer.Compress(COMPRESSION_MODE);
            switch (protocolType)
            {
                case Protocol.Tcp:
                    SendTCP(buffer);
                    break;
                case Protocol.Udp:
                    SendUDP(buffer);
                    break;
            }
        }

        protected async void SendTCP(byte[] buffer)
        {
            try
            {
                NetworkStream networkStream = _TCPSocket.GetStream();

                using (NeutronWriter writerOnly = new NeutronWriter())
                {
                    writerOnly.WriteFixedLength(buffer.Length);
                    writerOnly.Write(buffer);
                    byte[] nBuffer = writerOnly.ToArray();
                    //----------------------------------------------------------------------------
                    await networkStream.WriteAsync(nBuffer, 0, nBuffer.Length);
                    //else await networkStream.WriteAsync(buffer, 0, buffer.Length);
                }
            }
            catch (ObjectDisposedException) { Utils.Logger("Allocated memory released."); }
            catch (Exception ex) { Utils.StackTrace(ex); }
        }

        protected void SendUDP(byte[] message)
        {
            if (_UDPSocket == null || endPointUDP == null) return;
            try
            {
                _UDPSocket.BeginSend(message, message.Length, endPointUDP, (e) =>
                {
                    int data = ((UdpClient)(e.AsyncState)).EndSend(e);
                    if (data > 0) { }
                }, _UDPSocket);
            }
            catch (Exception ex) { Utils.LoggerError(ex.Message); }
        }

        //============================================================================================================//
        protected bool InitConnect()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Connected);
                writer.Write(_.IsBot);
                Send(writer.ToArray());
            }
            return true;
        }
        protected void InternalRPC(int RPCID, object[] parameters, SendTo sendTo, bool cached, Protocol protocolType, Broadcast broadcast)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                object[] bArray = { _.MyPlayer.ID, parameters };
                //---------------------------------------------------------------------------------------------------------------------
                writer.WritePacket(Packet.RPC);
                writer.WritePacket(broadcast);
                writer.WritePacket(sendTo);
                writer.Write(RPCID);
                writer.Write(cached);
                writer.Write(bArray.Serialize());
                //---------------------------------------------------------------------------------------------------------------------
                Send(writer.ToArray(), protocolType);
            }
        }

        protected void InternalRCC(MonoBehaviour mThis, int RCCID, byte[] parameters, bool enableCache, SendTo sendTo, Protocol protocolType, Broadcast broadcast)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Static);
                writer.WritePacket(broadcast);
                writer.WritePacket(sendTo);
                writer.Write(RCCID);
                writer.Write(enableCache);
                writer.Write(parameters);
                //---------------------------------------------------------------------------------------------------------------------
                Send(writer.ToArray(), protocolType);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Handles
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void HandleConnected(byte[] array)
        {
            _myPlayer = array.DeserializeObject<Player>();
        }

        //protected void HandleInstantiate (Vector3 pos, Quaternion rot, string playerPrefab, byte[] mPlayer) {
        //    //---------------------------------------------------------------------------------------------------------------------//
        //    Player playerInstantiated = mPlayer.DeserializeObject<Player> ();
        //    //---------------------------------------------------------------------------------------------------------------------//
        //    new Action (() => {
        //        GameObject playerPref = Resources.Load (playerPrefab, typeof (GameObject)) as GameObject;
        //        if (playerPref != null) {
        //            Neutron.onPlayerInstantiated (playerInstantiated, pos, rot, playerPref);
        //        } else Utils.LoggerError ($"CLIENT: -> Unable to load prefab {playerPrefab}", true);
        //    }).ExecuteOnMainThread (_, false);
        //}

        protected void HandleSendInput(byte[] mInput)
        {
            //    SerializableInput nInput = mInput.DeserializeObject<SerializableInput> ();
            //    //===================================================================
            //    SerializableVector3 nVelocity = nInput.Vector;
            //    //===================================================================
            //    Vector3 velocity = new Vector3 (nVelocity.x, nVelocity.y, nVelocity.z);
            //    //===================================================================
            //    //Neutron.Enqueue(() => playerRB.velocity = velocity);
        }

        protected void HandleRPC(int id, byte[] parameters)
        {
            object[] _array = parameters.DeserializeObject<object[]>();
            int senderID = (int)_array[0];
            object[] objectParams = (object[])_array[1];
            if (_.IsBot) return;
            new Action(() =>
            {
                if (playersObjects.TryGetValue(senderID, out NeutronView neutronObject))
                    Communication.InitRPC(id, objectParams, false, neutronObject);
            }).ExecuteOnMainThread(_);
        }

        protected void HandleRCC(int executeID, Player sender, byte[] parameters, bool isServer)
        {
            new Action(() =>
            {
                Communication.InitRCC(executeID, sender, parameters, isServer, _);
            }).ExecuteOnMainThread(_);
        }

        protected void HandleACC(int executeID, byte[] pParams)
        {
            new Action(() =>
            {
                Communication.InitACC(executeID, pParams);
            }).ExecuteOnMainThread(_);
        }

        protected void HandleAPC(int executeid, byte[] parameters, int playerID)
        {
            //-----------------------------------------------------------------------------
            if (_.IsBot) return;
            //-----------------------------------------------------------------------------
            new Action(() =>
            {
                if (playersObjects.TryGetValue(playerID, out NeutronView neutronObject))
                {
                    Communication.InitAPC(executeid, parameters, neutronObject);
                }
                //else Utils.LoggerError("APC: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
            }).ExecuteOnMainThread(_);
        }

        //protected void HandleDatabase(Packet packet, object[] response)
        //{
        //    if (_.onDatabasePacket != null)
        //    {
        //        new Action(() =>
        //        {
        //            _.onDatabasePacket(packet, response, _);
        //        }).ExecuteOnMainThread(_, false);
        //    }
        //    else Utils.LoggerError("onDatabasePacket event not registered.");
        //}

        protected void HandlePlayerDisconnected(Player player)
        {
            //-----------------------------------------------------------------------------------------
            if (_.IsBot) return;
            //-----------------------------------------------------------------------------------------
            if (playersObjects.TryGetValue(player.ID, out NeutronView neutronObject))
            {
                NeutronView obj = neutronObject;
                //-----------------------------------------------------------------------------------
                new Action(() =>
                {
                    MonoBehaviour.Destroy(obj.gameObject);
                }).ExecuteOnMainThread(_);
                //------------------------------------------------------------------------------------
                playersObjects.TryRemove(player.ID, out NeutronView objRemoved);
            }
            //else Utils.LoggerError("HPD: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
        }
        protected void HandleJsonProperties(int ownerID, string properties)
        {
            //----------------------------------------------------------------------------------
            if (_.IsBot) return;
            //----------------------------------------------------------------------------------
            if (playersObjects.TryGetValue(ownerID, out NeutronView neutronObject))
            {
                NeutronView obj = neutronObject;
                //-----------------------------------------------------------------------------------------------------------\\
                if (obj.neutronSyncBehaviour != null) JsonUtility.FromJsonOverwrite(properties, obj.neutronSyncBehaviour);
                else Utils.LoggerError("It was not possible to find a class that inherits from Neutron Sync Behavior.");
            }
            //else Utils.LoggerError("HJP: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
        }

        private void InitializeContainer()
        {
            if (!_isBot)
                Utils.CreateContainer("[Container] -> Player[Main]");
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnFailed(Packet packet, string errorMessage, Neutron localinstance)
        {
            Utils.LoggerError(packet + ":-> " + errorMessage);
        }

        private void OnPlayerJoinedChannel(Player player, Neutron localinstance)
        {
            UpdateLocalPlayer(player); // Update currentChannel
        }

        private void OnPlayerJoinedRoom(Player player, Neutron localinstance)
        {
            UpdateLocalPlayer(player); // update currentRoom
        }

        private void OnCreatedRoom(Room room, Neutron localinstance)
        {
            _.MyPlayer.currentRoom = room.ID;
        }

        private void OnDisconnected(string reason, Neutron localinstance)
        {
            _.Dispose();
            //-------------------------------------------------------------------
            Utils.Logger("You Have Disconnected from server -> [" + reason + "]");
        }

        private void UpdateLocalPlayer(Player player)
        {
            if (_.isLocalPlayer(player))
            {
                _myPlayer = player;
            }
        }

        private void OnConnected(bool success, Neutron localinstance)
        {
            if (success) InitializeContainer();
        }

        private void OnPlayerLeftRoom(Player player, Neutron localInstance)
        {

        }

        private void OnPlayerLeftChannel(Player player)
        {

        }
    }
}