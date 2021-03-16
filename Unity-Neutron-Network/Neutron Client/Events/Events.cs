using NeutronNetwork;
using UnityEngine;

namespace NeutronNetwork.Internal.Client.InternalEvents
{
    public class Events
    {
        /// <summary>
        /// This event is called when your connection to the server is established or fails.
        /// This call cannot perform functions that inherit from MonoBehaviour.
        ///This event is only triggered by you.
        /// </summary>
        /// <param name="success"></param>
        public delegate void OnNeutronConnected(bool success, Neutron localInstance);
        /// <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnNeutronDisconnected(string reason, Neutron localInstance);
        /// <summary>
        /// This event is called when you receive a message from yourself or other players.
        /// </summary>
        /// <param name="message"></param>
        /// <param name="sender"></param>
        public delegate void OnMessageReceived(string message, Player sender, Neutron localInstance);
        /// <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnDatabasePacket(Packet packet, object[] response, Neutron localInstance);
        /// <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnChannelsReceived(Channel[] channels, Neutron localInstance);
        /// <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnRoomsReceived(Room[] rooms, Neutron localInstance);
        /// <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnPlayerNicknameChanged(Player player, bool isMine, Neutron localInstance);
        /// <summary>
        /// This event is triggered when you or other players join the channel.
        /// This event is triggered by you and other players.
        /// </summary>
        /// <param name="player"></param>
        /// <param name="Channel"></param>
        public delegate void OnPlayerJoinedChannel(Player player, Neutron instance);
        public delegate void OnPlayerLeftChannel(Player player);
        /// <summary>
        /// This event is triggered by you and other players.
        /// </summary>
        public delegate void OnPlayerJoinedRoom(Player player, Neutron localInstance);
        public delegate void OnPlayerLeftRoom(Player player, Neutron localInstance);
        // <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnCreatedRoom(Room room, Neutron localInstance);
        // <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnFailed(Packet packet, string errorMessage, Neutron localInstance);
        /// <summary>
        /// This
        /// </summary>
        // <summary>
        ///This event is only triggered by you.
        /// <summary>
        public delegate void OnDestroyed(Neutron localInstance);
        public delegate void OnPlayerInstantiated(Player player, GameObject obj, Neutron localInstance);
        public delegate void OnPlayerPropertiesChanged(Player player, Neutron localInstance);
        public delegate void OnRoomPropertiesChanged(Player player, Neutron localInstance);
    }
}