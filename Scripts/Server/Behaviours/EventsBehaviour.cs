using System;
using UnityEngine;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;

namespace NeutronNetwork.Server.Internal
{
    /// <summary>
    /// You can implement your code here, or create a new script and inherit from this class, if you inherit from this script don't forget to remove this script and add yours and call "base".
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_EVENTS)]
    public class EventsBehaviour : MonoBehaviour
    {
        #region MonoBehaviour
        public void OnEnable()
        {
            #region Common Events
            NeutronServer.m_OnAwake.Add(OnServerAwake);
            NeutronServer.m_OnPlayerDisconnected.Add(OnPlayerDisconnected);
            #endregion

            #region Other
            MatchmakingHelper.OnCustomBroadcast.Add(OnCustomBroadcast);
            #endregion
        }

        public void OnDisable()
        {
            #region Common Events
            NeutronServer.m_OnAwake.Add(OnServerAwake);
            NeutronServer.m_OnPlayerDisconnected.Add(OnPlayerDisconnected);
            #endregion

            #region Other
            MatchmakingHelper.OnCustomBroadcast.Add(OnCustomBroadcast);
            #endregion
        }

        public virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }
        #endregion

        #region Handlers
        public virtual void OnPlayerDisconnected(NeutronPlayer nPlayer)
        {
            if (nPlayer.rPEndPoint != null)
                LogHelper.Info($"The Player [{nPlayer.rPEndPoint.ToString()}] Has been disconnected from server.");
        }

        public virtual void OnCheatDetected(NeutronPlayer nPlayer, string cheatName)
        {
            LogHelper.Info($"Usando hack porraaaaaaaa -> {nPlayer.Nickname}");
        }

        public virtual NeutronPlayer[] OnCustomBroadcast(NeutronPlayer nPlayer, TunnelingTo broadcast)
        {
            switch (broadcast)
            {
                default:
                    LogHelper.Error("Broadcast Packet not implemented! " + broadcast);
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
                SetOwner(channel, channel.ID);
                SceneHelper.CreateContainer($"[Container] -> Channel[{channel.ID}]", channel.Owner, channel.SceneSettings.enablePhysics, channel.SceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
                CreateDefaultRoomsContainer(channel);
            }
        }

        private void CreateDefaultRoomsContainer(NeutronChannel channel)
        {
            foreach (NeutronRoom room in channel.GetRooms())
            {
                channel.CountOfRooms++;
                SetOwner(room, channel.ID, room.ID);
                SceneHelper.CreateContainer($"[Container] -> Room[{room.ID}]", room.Owner, room.SceneSettings.enablePhysics, room.SceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
            }
        }

        private void SetOwner<T>(T AmbientType, int currentChannel, int currentRoom = -1) where T : INeutronMatchmaking
        {
            Type type = AmbientType.GetType();
            if (AmbientType.Owner == null)
            {
                NeutronPlayer owner = new NeutronPlayer();
                owner.IsServer = true;
                owner.Nickname = "Server";
                owner.ID = 0;
                if (owner != null)
                {
                    if (type == typeof(NeutronChannel))
                        owner.CurrentChannel = currentChannel;
                    else if (type == typeof(NeutronRoom))
                    {
                        owner.CurrentChannel = currentChannel;
                        owner.CurrentRoom = currentRoom;
                    }
                }
                owner.Matchmaking = MatchmakingHelper.Matchmaking(owner);
                AmbientType.Owner = owner;
            }
        }
        #endregion
    }
}