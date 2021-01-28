using UnityEngine;

public interface INeutronEvents
{
    void OnConnected(bool success, Neutron localInstance);
    void OnDisconnected(string reason, Neutron localInstance);
    void OnMessageReceived(string message, Player sender, Neutron localInstance);
    void OnDatabasePacket(Packet packet, object[] response, Neutron localInstance);
    void OnChannelsReceived(Channel[] channels, Neutron localInstance);
    void OnRoomsReceived(Room[] rooms, NeutronReader[] options, Neutron localInstance);
    void OnNicknameChanged(Neutron instance);
    void OnPlayerJoinedChannel(Player player, Neutron instance);
    void OnPlayerLeftChannel(Player player);
    void OnPlayerJoinedRoom(Player player, int room, Neutron localInstance);
    void OnPlayerLeftRoom(Player player, Neutron localInstance);
    void OnCreatedRoom(Room room, NeutronReader options, Neutron localInstance);
    void OnFailed(Packet packet, string errorMessage, Neutron localInstance);
    void OnDestroyed(Neutron localInstance);
    void OnPlayerInstantiated(Player player, GameObject obj, Neutron localInstance);
}