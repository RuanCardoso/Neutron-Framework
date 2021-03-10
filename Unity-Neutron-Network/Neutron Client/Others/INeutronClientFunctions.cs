using NeutronNetwork;
using NeutronNetwork.Internal.Comms;
using NeutronNetwork.Internal.Extesions;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork.Internal.Client
{
    public class NeutronClientFunctions : NeutronClientConstants
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
            if (!_.IsConnected) return;
            switch (protocolType)
            {
                case Protocol.Tcp:
                    SendTCP(buffer);
                    break;
                case Protocol.Udp:
                    SendUDP(buffer);
                    break;
            }
            Utils.UpdateStatistics(Statistics.ClientSent, buffer.Length);
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
                    await networkStream.WriteAsync(nBuffer, 0, nBuffer.Length);
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }

        protected async void SendUDP(byte[] message)
        {
            try
            {
                await _UDPSocket.SendAsync(message, message.Length, endPointUDP);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }

        protected void InitConnect()
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Connected);
                writer.Write(_.IsBot);
                Send(writer.ToArray());
            }
        }
        
        protected void InternalRPC(int playerID, int RPCID, byte[] parameters, SendTo sendTo, bool cached, Protocol protocolType, Broadcast broadcast)
        {
            NeutronMessageInfo infor = _myPlayer.infor;
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.RPC);
                writer.WritePacket(broadcast);
                writer.WritePacket(sendTo);
                writer.Write(playerID);
                writer.Write(RPCID);
                writer.Write(cached);
                writer.WriteExactly(parameters);
                writer.WriteExactly(infor.Serialize());
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
                writer.WriteExactly(parameters);
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

        protected void HandleRPC(int rpcID, int playerID, byte[] parameters, Player sender, NeutronMessageInfo infor)
        {
            if (_.IsBot) return;
            new Action(() =>
            {
                if (playersObjects.TryGetValue(playerID, out NeutronView neutronObject))
                    Communication.InitRPC(rpcID, parameters, sender, infor, neutronObject);
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
                Communication.InitResponse(executeID, pParams);
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
                //else Utilities.LoggerError("APC: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
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
        //    else Utilities.LoggerError("onDatabasePacket event not registered.");
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
            //else Utilities.LoggerError("HPD: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
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
                if (obj.neutronSyncBehaviour != null)
                {
                    var sync = obj.neutronSyncBehaviour;
                    JsonConvert.PopulateObject(properties, sync, new JsonSerializerSettings()
                    {
                        ObjectCreationHandling = ObjectCreationHandling.Replace
                    });
                    JsonUtility.FromJsonOverwrite(properties, sync);
                }
                else Utilities.LoggerError("It was not possible to find a class that inherits from Neutron Sync Behavior.");
            }
            //else Utilities.LoggerError("HJP: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
        }

        private void InitializeContainer()
        {
            if (!_isBot)
                Utils.CreateContainer("[Container] -> Player[Main]");
        }

        private void UpdateLocalPlayer(Player player)
        {
            if (_.isLocalPlayer(player))
            {
                _myPlayer = player;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnFailed(Packet packet, string errorMessage, Neutron localinstance)
        {
            Utilities.LoggerError(packet + ":-> " + errorMessage);
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
            _.MyPlayer.CurrentRoom = room.ID;
        }

        private void OnDisconnected(string reason, Neutron localinstance)
        {
            _.Dispose();
            //-------------------------------------------------------------------
            Utilities.Logger("You Have Disconnected from server -> [" + reason + "]");
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