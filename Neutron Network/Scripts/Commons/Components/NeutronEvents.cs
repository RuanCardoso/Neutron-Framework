using System;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Extesions;
using NeutronNetwork.Internal.Server.Cheats;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.Delegates
{
    /// <summary>
    /// You can implement your code here, or create a new script and inherit from this class, if you inherit from this script don't forget to remove this script and add yours and call "base".
    /// </summary>
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_EVENTS_ORDER)]
    public class NeutronEvents : MonoBehaviour
    {
        public void OnEnable()
        {
            #region Common Events
            NeutronServer.m_OnAwake += OnServerAwake;
            NeutronServer.m_OnPlayerDisconnected += OnPlayerDisconnected;
            #endregion

            #region Cheat Events
            CheatsUtils.onCheatDetected += OnCheatDetected;
            #endregion
        }

        public void OnDisable()
        {
            #region Common Events
            NeutronServer.m_OnAwake -= OnServerAwake;
            #endregion

            #region Cheat Events
            CheatsUtils.onCheatDetected -= OnCheatDetected;
            #endregion
        }

        public virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }

        public virtual void OnPlayerDisconnected(Player nPlayer)
        {
            NeutronUtils.Logger($"The Player [{nPlayer.RemoteEndPoint().ToString()}] Has been disconnected from server.");
        }

        public virtual void OnCheatDetected(Player nPlayer, string cheatName)
        {
            NeutronUtils.Logger($"Usando hack porraaaaaaaa -> {nPlayer.Nickname}");
        }

        #region Internal
        private void CreateDefaultContainer() => InternalUtils.CreateContainer($"[Container] -> Server");
        private void CreateDefaultChannelsContainer()
        {
            foreach (var channel in Neutron.Server.ChannelsById.Values)
            {
                SetOwner(channel, channel.ID);
                InternalUtils.CreateContainer($"[Container] -> Channel[{channel.ID}]", channel.SceneSettings.clientOnly, channel.Owner, channel.SceneSettings.enablePhysics, channel.SceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
                CreateDefaultRoomsContainer(channel);
            }
        }
        private void CreateDefaultRoomsContainer(Channel channel)
        {
            foreach (Room room in channel.GetRooms())
            {
                channel.CountOfRooms++;
                SetOwner(room, channel.ID, room.ID);
                InternalUtils.CreateContainer($"[Container] -> Room[{room.ID}]", room.SceneSettings.clientOnly, room.Owner, room.SceneSettings.enablePhysics, room.SceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
            }
        }
        private void SetOwner<T>(T AmbientType, int currentChannel, int currentRoom = -1) where T : INeutronMatchmaking
        {
            Type type = AmbientType.GetType();
            if (AmbientType.Owner == null)
            {
                Player owner = new Player();
                owner.isServer = true;
                if (owner != null)
                {
                    owner.Nickname = "Server";
                    if (type == typeof(Channel))
                    {
                        owner.CurrentChannel = currentChannel;
                    }
                    else if (type == typeof(Room))
                    {
                        owner.CurrentChannel = currentChannel;
                        owner.CurrentRoom = currentRoom;
                    }
                }
                AmbientType.Owner = owner;
            }
        }
        #endregion
    }
}