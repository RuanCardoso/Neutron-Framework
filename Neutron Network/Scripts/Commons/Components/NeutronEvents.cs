using System;
using NeutronNetwork.Internal.Server.Cheats;
using UnityEngine;

namespace NeutronNetwork.Internal.Server.InternalEvents
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

        private void CreateDefaultContainer() => Utils.CreateContainer($"[Container] -> Server", false, false, null, null, Neutron.Server.PhysicsMode);
        private void CreateDefaultChannelsContainer()
        {
            for (int i = 0; i < Neutron.Server._Channels.Count; i++)
            {
                Channel channel = Neutron.Server._Channels[i];
                if (Neutron.Server.ChannelsById.TryAdd(channel.ID, channel))
                {
                    Utils.CreateContainer($"[Container] -> Channel[{channel.ID}]", Neutron.Server.ChannelPhysics, Neutron.Server.SharingOnChannels, Neutron.Server.sharedObjects, Neutron.Server.unsharedObjects, Neutron.Server.PhysicsMode);
                    CreateDefaultRoomsContainer(channel);
                }
            }
        }

        private void CreateDefaultRoomsContainer(Channel channel)
        {
            foreach (Room room in channel.GetRooms())
                Utils.CreateContainer($"[Container] -> Room[{room.ID}]", Neutron.Server.RoomPhysics, Neutron.Server.SharingOnRooms, Neutron.Server.sharedObjects, Neutron.Server.unsharedObjects, Neutron.Server.PhysicsMode);
        }
    }
}