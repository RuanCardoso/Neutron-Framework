using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    /// You can implement your code here, or create a new script and inherit from this class, if you inherit from this script don't forget to remove this script and add yours and call "base".
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_EVENTS)]
    public class EventsBehaviour : MonoBehaviour
    {
        #region MonoBehaviour
        protected virtual void OnEnable()
        {
            ServerBase.OnAwake += OnServerAwake;
            ServerBase.OnCustomTunneling += OnCustomBroadcast;
            ServerBase.OnMessageReceived += OnMessageReceived;
            ServerBase.OnPlayerCreatedRoom += OnPlayerCreatedRoom;
            ServerBase.OnPlayerDestroyed += OnPlayerDestroyed;
            ServerBase.OnPlayerDisconnected += OnPlayerDisconnected;
            ServerBase.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            ServerBase.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            ServerBase.OnPlayerLeftChannel += OnPlayerLeftChannel;
            ServerBase.OnPlayerLeftRoom += OnPlayerLeftRoom;
            ServerBase.OnPlayerNicknameChanged += OnPlayerNicknameChanged;
            ServerBase.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
            ServerBase.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
            ServerBase.OnAuthentication += OnAuthentication;
            ServerBase.OnReceivePacket += OnReceivePacket;
        }

        protected virtual void OnDisable()
        {
            ServerBase.OnAwake -= OnServerAwake;
            ServerBase.OnCustomTunneling -= OnCustomBroadcast;
            ServerBase.OnMessageReceived -= OnMessageReceived;
            ServerBase.OnPlayerCreatedRoom -= OnPlayerCreatedRoom;
            ServerBase.OnPlayerDestroyed -= OnPlayerDestroyed;
            ServerBase.OnPlayerDisconnected -= OnPlayerDisconnected;
            ServerBase.OnPlayerJoinedChannel -= OnPlayerJoinedChannel;
            ServerBase.OnPlayerJoinedRoom -= OnPlayerJoinedRoom;
            ServerBase.OnPlayerLeftChannel -= OnPlayerLeftChannel;
            ServerBase.OnPlayerLeftRoom -= OnPlayerLeftRoom;
            ServerBase.OnPlayerNicknameChanged -= OnPlayerNicknameChanged;
            ServerBase.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            ServerBase.OnRoomPropertiesChanged -= OnRoomPropertiesChanged;
            ServerBase.OnAuthentication -= OnAuthentication;
            ServerBase.OnReceivePacket -= OnReceivePacket;
        }

        protected virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }
        #endregion

        #region Registered Methods
        protected virtual bool OnReceivePacket(Packet packet)
        {
            return true;
        }

        protected virtual async Task<bool> OnAuthentication(NeutronPlayer player, Authentication authentication)
        {
            return await Task.Run(() => true);
        }

#pragma warning disable UNT0006
        protected virtual void OnPlayerDisconnected(NeutronPlayer player)
#pragma warning restore UNT0006
        {
            LogHelper.Info($"The Player [{player.StateObject.TcpRemoteEndPoint}] Has been disconnected from server.");
        }

        protected virtual void OnCheatDetected(NeutronPlayer player, string name)
        {
            LogHelper.Info($"Usando hack porraaaaaaaa -> {player.Nickname}");
        }

        protected virtual bool OnPlayerCreatedRoom(NeutronPlayer player, NeutronRoom room)
        {
            NeutronSchedule.ScheduleTask(() =>
            {
                SceneHelper.CreateContainer($"[Container] -> Room[{room.ID}]", room.Player, room.SceneView.HasPhysics, room.SceneView.GameObjects, Neutron.Server.Physics);
            });
            return true;
        }

        protected virtual bool OnRoomPropertiesChanged(NeutronPlayer player, string properties)
        {
            return true;
        }

        protected virtual bool OnPlayerPropertiesChanged(NeutronPlayer player, string propertie)
        {
            return true;
        }

        protected virtual bool OnPlayerNicknameChanged(NeutronPlayer player, string nickname)
        {
            return true;
        }

        protected virtual void OnPlayerLeftRoom(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerLeftChannel(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerJoinedRoom(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerJoinedChannel(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerDestroyed(NeutronPlayer player)
        {

        }

        protected virtual bool OnMessageReceived(NeutronPlayer player, string message)
        {
            return true;
        }

        protected virtual NeutronPlayer[] OnCustomBroadcast(NeutronPlayer player, TunnelingTo tunnelingTo)
        {
            switch (tunnelingTo)
            {
                default:
                    LogHelper.Error("Broadcast Packet not implemented! " + tunnelingTo);
                    return null;
            }
        }
        #endregion

        #region Internal
        private void CreateDefaultContainer() => SceneHelper.CreateContainer($"[Container] -> Server");

        private void CreateDefaultChannelsContainer()
        {
            foreach (var channel in Neutron.Server.ChannelsById.Values)
            {
                SetOwner(channel, channel);
                SceneHelper.CreateContainer($"[Container] -> Channel[{channel.ID}]", channel.Player, channel.SceneView.HasPhysics, channel.SceneView.GameObjects, Neutron.Server.Physics);
                CreateDefaultRoomsContainer(channel);
            }
        }

        private void CreateDefaultRoomsContainer(NeutronChannel channel)
        {
            foreach (NeutronRoom room in channel.GetRooms())
            {
                SetOwner(room, channel, room);
                SceneHelper.CreateContainer($"[Container] -> Room[{room.ID}]", room.Player, room.SceneView.HasPhysics, room.SceneView.GameObjects, Neutron.Server.Physics);
            }
        }

        private void SetOwner<T>(T matchmakingMode, NeutronChannel channel, NeutronRoom room = null) where T : INeutronMatchmaking
        {
            Type type = matchmakingMode.GetType();
            if (matchmakingMode.Player == null)
            {
                NeutronPlayer player = new NeutronPlayer
                {
                    IsServer = true,
                    Nickname = "Server",
                    ID = 0
                };
                //************************************************************************
                if (type == typeof(NeutronChannel))
                    player.Channel = channel;
                else if (type == typeof(NeutronRoom))
                {
                    player.Channel = channel;
                    player.Room = room;
                }
                //************************************************************************
                player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                //************************************************************************
                matchmakingMode.Player = player;
            }
        }

        /// <summary>
        ///* Envia o estado da autenticação ao seu usuário.
        /// </summary>
        /// <param name="user">* O usuário a ser autenticado.</param>
        /// <param name="properties">* As propriedades(Json) que serão enviadas ao usuário autenticado.</param>
        /// <param name="status">* O estado da autenticação</param>
        /// <returns></returns>
        protected bool OnAuth(NeutronPlayer user, string properties, bool status)
        {
            using (NeutronWriter auth = Neutron.PooledNetworkWriters.Pull())
            {
                auth.WritePacket((byte)Packet.AuthStatus);
                auth.Write(properties);
                auth.Write(status);
                user.Write(auth);
            }
            return status;
        }
        #endregion
    }
}