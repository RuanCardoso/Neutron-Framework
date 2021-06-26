using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using Newtonsoft.Json;
using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Threading;
using UnityEngine;

namespace NeutronNetwork.Client
{
    public class NeutronClientFunctions : NeutronClientConstants
    {
        /// <summary>
        /// Get instance of derived class.
        /// </summary>
        private Neutron _;
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
            //--------------------------------------------------------------
            Neutron.OnNeutronConnected.Register(OnConnected);
            //_.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            //_.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            //_.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            //_.OnPlayerLeftChannel += OnPlayerLeftChannel;
            //_.OnPlayerLeftRoom += OnPlayerLeftRoom;
            //_.OnFailed += OnFailed;
            //_.OnNeutronDisconnected += OnDisconnected;
            //_.OnCreatedRoom += OnCreatedRoom;
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
        }

        protected async void SendTCP(byte[] buffer)
        {
            try
            {
                NetworkStream networkStream = TcpSocket.GetStream();
                using (NeutronWriter writerOnly = Neutron.PooledNetworkWriters.Pull())
                {
                    writerOnly.SetLength(0);
                    writerOnly.WriteFixedLength(buffer.Length);
                    writerOnly.Write(buffer);
                    byte[] nBuffer = writerOnly.ToArray();
                    await networkStream.WriteAsync(nBuffer, 0, nBuffer.Length);
                    NeutronStatistics.m_ClientTCP.AddOutgoing(buffer.Length);
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
                await UdpSocket.SendAsync(message, message.Length, UDPEndPoint);
                NeutronStatistics.m_ClientUDP.AddOutgoing(message.Length);
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }

        protected void Handshake()
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Handshake);
                Send(writer.ToArray());
            }
        }

        protected void InternalRPC(int nID, int dynamicID, byte[] parameters, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            NeutronMessageInfo infor = _myPlayer.infor;
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.iRPC);
                writer.WritePacket(broadcast);
                writer.WritePacket(sendTo);
                writer.WritePacket(cacheMode);
                writer.Write(nID);
                writer.Write(dynamicID);
                writer.WriteExactly(parameters);
                //writer.WriteExactly(infor);
                Send(writer.ToArray(), protocolType);
            }
        }

        protected void InternalRCC(int nID, int nonDynamic, byte[] parameters, Protocol protocolType)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.sRPC);
                writer.Write(nID);
                writer.Write(nonDynamic);
                writer.WriteExactly(parameters);
                //---------------------------------------------------------------------------------------------------------------------
                Send(writer.ToArray(), protocolType);
            }
        }
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        //Handles
        ////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////
        protected void HandleConnected(Player nPlayer)
        {
            _myPlayer = nPlayer;
            PlayerConnections[_myPlayer.ID] = _myPlayer;
            void RegisterSceneObjects()
            {
                foreach (NeutronView nV in GameObject.FindObjectsOfType<NeutronView>().Where(x => x.IsSceneObject && !x.IsServer))
                    NeutronRegister.RegisterSceneObject(_myPlayer, nV, false, _);
            }
            NeutronDispatcher.Dispatch(() =>
            {
                RegisterSceneObjects();
            });
        }

        protected void iRPCHandler(int rpcID, int playerID, byte[] parameters, bool isMine, Player sender, NeutronMessageInfo infor)
        {
            if (NetworkObjects.TryGetValue(playerID, out NeutronView neutronObject))
            {
                if (neutronObject.Dynamics.TryGetValue(rpcID, out RemoteProceduralCall remoteProceduralCall))
                {
                    iRPC dynamicAttr = (iRPC)remoteProceduralCall.attribute;
                    Action _ = new Action(() =>
                    {
                        NeutronHelper.iRPC(parameters, isMine, remoteProceduralCall, sender, infor, neutronObject);
                    });
                    if (dynamicAttr.DispatchOnMainThread)
                        NeutronDispatcher.Dispatch(_);
                    else _.Invoke();
                }
                else NeutronLogger.LoggerError("Invalid iRPC ID, there is no attribute with this ID in the target object.");
            }
        }

        protected void sRPCHandler(int executeID, Player sender, byte[] parameters, bool isServer, bool isMine)
        {
            NeutronDispatcher.Dispatch(() =>
            {
                if (NeutronNonDynamicBehaviour.NonDynamics.TryGetValue(executeID, out RemoteProceduralCall remoteProceduralCall))
                {
                    NeutronHelper.sRPC(executeID, sender, parameters, remoteProceduralCall, isServer, isMine, _);
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
            //-----------------------------------------------------------------------------------------
            if (NetworkObjects.TryGetValue(player.ID, out NeutronView neutronObject))
            {
                NeutronView obj = neutronObject;
                //-----------------------------------------------------------------------------------
                NeutronDispatcher.Dispatch(() =>
                {
                    MonoBehaviour.Destroy(obj.gameObject);
                });
                //------------------------------------------------------------------------------------
                NetworkObjects.TryRemove(player.ID, out NeutronView objRemoved);
            }
        }

        private void InitializeContainer()
        {
            SceneHelper.CreateContainer(NeutronConstants.CONTAINER_PLAYER_NAME);
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

        private void OnFailed(SystemPacket packet, string errorMessage, Neutron localinstance)
        {
            NeutronLogger.LoggerError(packet + ":-> " + errorMessage);
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
            //_.Dispose();
            //-------------------------------------------------------------------
            NeutronLogger.Logger("You Have Disconnected from server -> [" + reason + "]");
        }

        private void OnConnected(bool success, Neutron localinstance)
        {
            if (success)
                InitializeContainer();
        }

        private void OnPlayerLeftRoom(Player player, Neutron localInstance)
        {

        }

        private void OnPlayerLeftChannel(Player player)
        {

        }
    }
}