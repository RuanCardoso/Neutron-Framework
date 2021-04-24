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
            InternalUtils.UpdateStatistics(Statistics.ClientSent, buffer.Length);
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

        protected void InternalRPC(int nID, int dynamicID, byte[] parameters, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            NeutronMessageInfo infor = _myPlayer.infor;
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.Dynamic);
                writer.WritePacket(broadcast);
                writer.WritePacket(sendTo);
                writer.WritePacket(cacheMode);
                writer.Write(nID);
                writer.Write(dynamicID);
                writer.WriteExactly(parameters);
                writer.WriteExactly(infor.Serialize());
                Send(writer.ToArray(), protocolType);
            }
        }

        protected void InternalRCC(int ID, byte[] parameters, Protocol protocolType)
        {
            using (NeutronWriter writer = new NeutronWriter())
            {
                writer.WritePacket(Packet.NonDynamic);
                writer.Write(ID);
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
            _myPlayer = array.Deserialize<Player>();
            playerConnections[_myPlayer.ID] = _myPlayer;
            Debug.LogError(playerConnections[_myPlayer.ID].Get["Neutron"]);
            Debug.LogError(_myPlayer.Get["Neutron"]);
            void RegisterSceneObjects()
            {
                foreach (NeutronView nV in GameObject.FindObjectsOfType<NeutronView>().Where(x => x.IsSceneObject && !x.isServer))
                    NeutronRegister.RegisterSceneObject(_myPlayer, nV, false, _);
            }
            NeutronDispatcher.Dispatch(() =>
            {
                RegisterSceneObjects();
            });
        }

        protected void HandleRPC(int rpcID, int playerID, byte[] parameters, Player sender, NeutronMessageInfo infor)
        {
            if (_.IsBot) return;
            if (networkObjects.TryGetValue(playerID, out NeutronView neutronObject))
            {
                if (neutronObject.Dynamics.TryGetValue(rpcID, out RemoteProceduralCall remoteProceduralCall))
                {
                    Dynamic dynamicAttr = (Dynamic)remoteProceduralCall.attribute;
                    Action _ = new Action(() =>
                    {
                        Communication.Dynamic(rpcID, parameters, remoteProceduralCall, sender, infor, neutronObject);
                    });
                    if (dynamicAttr.DispatchOnMainThread)
                        NeutronDispatcher.Dispatch(_);
                    else _.Invoke();
                }
                else NeutronUtils.LoggerError("Invalid Dynamic ID, there is no attribute with this ID in the target object.");
            }
        }

        protected void HandleRCC(int executeID, Player sender, byte[] parameters, bool isServer)
        {
            NeutronDispatcher.Dispatch(() =>
            {
                if (NeutronNonDynamicBehaviour.NonDynamics.TryGetValue(executeID, out RemoteProceduralCall remoteProceduralCall))
                {
                    Communication.NonDynamic(executeID, sender, parameters, remoteProceduralCall, isServer, _);
                }
            });
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
        //}

        protected void HandlePlayerDisconnected(Player player)
        {
            //-----------------------------------------------------------------------------------------
            if (_.IsBot) return;
            //-----------------------------------------------------------------------------------------
            if (networkObjects.TryGetValue(player.ID, out NeutronView neutronObject))
            {
                NeutronView obj = neutronObject;
                //-----------------------------------------------------------------------------------
                NeutronDispatcher.Dispatch(() =>
                {
                    MonoBehaviour.Destroy(obj.gameObject);
                });
                //------------------------------------------------------------------------------------
                networkObjects.TryRemove(player.ID, out NeutronView objRemoved);
            }
        }

        private void InitializeContainer()
        {
            if (!_isBot)
                InternalUtils.CreateContainer(Neutron.CONTAINER_NAME);
        }

        private void UpdateLocalPlayer(Player player)
        {
            if (_.IsMine(player))
            {
                _myPlayer = player;
            }
        }

        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Events
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////

        private void OnFailed(Packet packet, string errorMessage, Neutron localinstance)
        {
            NeutronUtils.LoggerError(packet + ":-> " + errorMessage);
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
            NeutronUtils.Logger("You Have Disconnected from server -> [" + reason + "]");
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