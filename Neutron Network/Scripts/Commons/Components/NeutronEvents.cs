using System;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server.Cheats;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.Delegates
{
    public class NeutronEvents : MonoBehaviour
    {
        public void Initialize()
        {
            #region Common Events
            NeutronServer.onServerAwake += OnServerAwake;
            #endregion

            #region Cheat Events
            CheatsUtils.onCheatDetected += OnCheatDetected;
            #endregion
        }

        public virtual void OnServerAwake()
        {
            CreateDefaultContainer();
            CreateDefaultChannelsContainer();
        }

        public virtual void OnCheatDetected(Player playerDetected, string cheatName)
        {
            NeutronUtils.Logger($"Hm detectei alguem safado -> {playerDetected.Nickname}");
        }

        #region Internal
        private void CreateDefaultContainer() => InternalUtils.CreateContainer($"[Container] -> Server");
        private void CreateDefaultChannelsContainer()
        {
            for (int i = 0; i < Neutron.Server._Channels.Count; i++)
            {
                Channel channel = Neutron.Server._Channels[i];
                if (Neutron.Server.ChannelsById.TryAdd(channel.ID, channel))
                {
                    SetOwner(channel, channel.ID);
                    InternalUtils.CreateContainer($"[Container] -> Channel[{channel.ID}]", channel.sceneSettings.clientOnly, channel.Owner, channel.sceneSettings.enablePhysics, channel.sceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
                    CreateDefaultRoomsContainer(channel);
                }
            }
        }
        private void CreateDefaultRoomsContainer(Channel channel)
        {
            foreach (Room room in channel.GetRooms())
            {
                SetOwner(room, channel.ID, room.ID);
                InternalUtils.CreateContainer($"[Container] -> Room[{room.ID}]", room.sceneSettings.clientOnly, room.Owner, room.sceneSettings.enablePhysics, room.sceneSettings.sceneObjects, Neutron.Server.PhysicsMode);
            }
        }
        private void SetOwner<T>(T AmbientType, int currentChannel, int currentRoom = -1) where T : INeutronOwner
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