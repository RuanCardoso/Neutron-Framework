using UnityEngine;

public class SEvents {
    public delegate void OnPlayerDisconnected (Player playerDisconnected);
    public delegate void OnPlayerInstantiated (Player playerInstantiated);
    public delegate void OnPlayerDestroyed (Player playerDestroyed);
    public delegate void OnPlayerJoinedChannel (Player playerJoined);
    public delegate void OnPlayerLeaveChannel (Player playerLeave);
    public delegate void OnPlayerJoinedRoom (Player playerJoined);
    public delegate void OnPlayerLeaveRoom (Player playerLeave);
    public delegate void OnCheatDetected (Player playerDetected, string cheatName);
    public delegate void OnPlayerPropertiesChanged (Player player, NeutronSyncBehaviour properties, string propertieName, Broadcast broadcast);
    public delegate void OnPlayerCollision (Player player, Collision coll, string type);
    public delegate void OnPlayerTrigger (Player player, Collider coll, string type);
    public delegate void OnServerAwake ();
    public delegate void OnServerStart ();
}