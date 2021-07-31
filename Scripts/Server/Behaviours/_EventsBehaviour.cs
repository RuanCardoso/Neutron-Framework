using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Interfaces;
using System;
using UnityEngine;

namespace NeutronNetwork.Server.Internal
{
    /// <summary>
    /// You can implement your code here, or create a new script and inherit from this class, if you inherit from this script don't forget to remove this script and add yours and call "base".
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_EVENTS)]
    public class EventsBehaviour : MonoBehaviour
    {
        #region MonoBehaviour
        public virtual void OnEnable()
        {
            #region Common Events
            NeutronServer.OnAwake += OnServerAwake;
            NeutronServer.OnPlayerDisconnected += OnPlayerDisconnected;
            #endregion

            #region Other
            MatchmakingHelper.OnCustomBroadcast += OnCustomBroadcast;
            #endregion
        }

        public virtual void OnDisable()
        {
            #region Common Events
            NeutronServer.OnAwake -= OnServerAwake;
            NeutronServer.OnPlayerDisconnected -= OnPlayerDisconnected;
            #endregion

            #region Other
            MatchmakingHelper.OnCustomBroadcast -= OnCustomBroadcast;
            #endregion
        }

        public virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }
        #endregion

        #region Handlers
        public virtual void OnPlayerDisconnected(NeutronPlayer player)
        {
            if (player.RemoteEndPoint != null)
                LogHelper.Info($"The Player [{player.RemoteEndPoint}] Has been disconnected from server.");
        }

        public virtual void OnCheatDetected(NeutronPlayer player, string name)
        {
            LogHelper.Info($"Usando hack porraaaaaaaa -> {player.Nickname}");
        }

        public virtual NeutronPlayer[] OnCustomBroadcast(NeutronPlayer player, TunnelingTo tunnelingTo)
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
                channel.RoomCount++;
                SetOwner(room, channel, room);
                SceneHelper.CreateContainer($"[Container] -> Room[{room.ID}]", room.Player, room.SceneView.HasPhysics, room.SceneView.GameObjects, Neutron.Server.Physics);
            }
        }

        private void SetOwner<T>(T AmbientType, NeutronChannel currentChannel, NeutronRoom currentRoom = null) where T : INeutronMatchmaking
        {
            Type type = AmbientType.GetType();
            if (AmbientType.Player == null)
            {
                NeutronPlayer owner = new NeutronPlayer();
                owner.IsServer = true;
                owner.Nickname = "Server";
                owner.ID = 0;
                if (owner != null)
                {
                    if (type == typeof(NeutronChannel))
                        owner.Channel = currentChannel;
                    else if (type == typeof(NeutronRoom))
                    {
                        owner.Channel = currentChannel;
                        owner.Room = currentRoom;
                    }
                }
                owner.Matchmaking = MatchmakingHelper.Matchmaking(owner);
                AmbientType.Player = owner;
            }
        }
        #endregion
    }
}