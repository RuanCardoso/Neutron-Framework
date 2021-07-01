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
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_EVENTS)]
    public class NeutronEvents : MonoBehaviour
    {
        #region MonoBehaviour
        public void OnEnable()
        {
            #region Common Events
            NeutronServer.m_OnAwake += OnServerAwake;
            NeutronServer.m_OnPlayerDisconnected += OnPlayerDisconnected;
            #endregion

            #region Cheat Events
            CheatsHelper.m_OnCheatDetected += OnCheatDetected;
            #endregion

            #region Other
            MatchmakingHelper.m_OnCustomBroadcast += OnCustomBroadcast;
            #endregion
        }

        public void OnDisable()
        {
            #region Common Events
            NeutronServer.m_OnAwake -= OnServerAwake;
            #endregion

            #region Cheat Events
            CheatsHelper.m_OnCheatDetected -= OnCheatDetected;
            #endregion
        }

        public virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }
        #endregion

        #region Handlers
        public virtual void OnPlayerDisconnected(Player nPlayer)
        {
            NeutronLogger.Logger($"The Player [{nPlayer.RemoteEndPoint().ToString()}] Has been disconnected from server.");
        }

        public virtual void OnCheatDetected(Player nPlayer, string cheatName)
        {
            NeutronLogger.Logger($"Usando hack porraaaaaaaa -> {nPlayer.Nickname}");
        }

        public virtual Player[] OnCustomBroadcast(Player nPlayer, Broadcast broadcast)
        {
            switch (broadcast)
            {
                default:
                    NeutronLogger.LoggerError("Broadcast Packet not implemented! " + broadcast);
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

        private void CreateDefaultRoomsContainer(Channel channel)
        {
            foreach (Room room in channel.GetRooms())
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
                Player owner = new Player();
                owner.IsServer = true;
                owner.Nickname = "Server";
                owner.ID = 0;
                if (owner != null)
                {
                    if (type == typeof(Channel))
                        owner.CurrentChannel = currentChannel;
                    else if (type == typeof(Room))
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