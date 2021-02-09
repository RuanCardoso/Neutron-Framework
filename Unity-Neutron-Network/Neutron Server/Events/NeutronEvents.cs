using UnityEngine;

namespace NeutronNetwork.Internal.Server.InternalEvents
{
    public class NeutronEvents : MonoBehaviour
    {
        void OnEnable()
        {
            NeutronSFunc.onServerStart += OnServerStart;
            NeutronSFunc.onPlayerDisconnected += OnPlayerDisconnected;
            NeutronSFunc.onPlayerInstantiated += OnPlayerInstantiated;
            NeutronSFunc.onPlayerDestroyed += OnPlayerDestroyed;
            NeutronSFunc.onPlayerJoinedChannel += OnPlayerJoinedChannel;
            NeutronSFunc.onPlayerLeaveChannel += OnPlayerLeaveChannel;
            NeutronSFunc.onPlayerJoinedRoom += OnPlayerJoinedRoom;
            NeutronSFunc.onPlayerLeaveRoom += OnPlayerLeaveRoom;
            NeutronSFunc.onChanged += OnPlayerPropertiesChanged;
            NeutronSFunc.onCheatDetected += OnCheatDetected;
            //----------------------------------------------------------------------------
            ServerOnCollisionEvents.onPlayerCollision += OnPlayerCollision;
            ServerOnCollisionEvents.onPlayerTrigger += OnPlayerTrigger;
        }
        void OnDisable()
        {
            NeutronSFunc.onServerStart -= OnServerStart;
            NeutronSFunc.onPlayerDisconnected -= OnPlayerDisconnected;
            NeutronSFunc.onPlayerInstantiated -= OnPlayerInstantiated;
            NeutronSFunc.onPlayerDestroyed -= OnPlayerDestroyed;
            NeutronSFunc.onPlayerJoinedChannel -= OnPlayerJoinedChannel;
            NeutronSFunc.onPlayerLeaveChannel -= OnPlayerLeaveChannel;
            NeutronSFunc.onPlayerJoinedRoom -= OnPlayerJoinedRoom;
            NeutronSFunc.onPlayerLeaveRoom -= OnPlayerLeaveRoom;
            NeutronSFunc.onChanged -= OnPlayerPropertiesChanged;
            NeutronSFunc.onCheatDetected -= OnCheatDetected;
            //----------------------------------------------------------------------------
            ServerOnCollisionEvents.onPlayerCollision -= OnPlayerCollision;
            ServerOnCollisionEvents.onPlayerTrigger -= OnPlayerTrigger;
        }

        private void OnServerStart()
        {

        }

        private void Awake()
        {

        }

        private void OnPlayerTrigger(Player player, Collider coll, string type)
        {

        }

        private void OnPlayerCollision(Player mPlayer, Collision coll, string type)
        {

        }

        private void OnCheatDetected(Player playerDetected, System.String cheatName)
        {
            string detectedPlayer = $"Cheat detected -> {cheatName} -> {playerDetected.Nickname}";

            Utils.LoggerError(detectedPlayer);

            NeutronSFunc.SendErrorMessage(playerDetected, Packet.RPC, detectedPlayer);
        }

        private void OnPlayerPropertiesChanged(Player mPlayer, NeutronSyncBehaviour properties, System.String propertieName, Broadcast broadcast)
        {
            //NeutronSFunc.SendProperties(mPlayer, properties, SendTo.All, Broadcast.Channel);
        }

        private void OnPlayerLeaveRoom(Player playerLeave)
        {

        }

        private void OnPlayerJoinedRoom(Player playerJoined)
        {

        }

        private void OnPlayerLeaveChannel(Player playerLeave)
        {

        }

        private void OnPlayerJoinedChannel(Player playerJoined)
        {
            //new Action(() =>
            //{
            //    Connection mainConnection = FindObjectOfType<Connection>();
            //    //-------------------------------------------
            //    mainConnection.MultipleBots(playerJoined.currentChannel);
            //}).ExecuteOnMainThread();
        }

        private void OnPlayerDestroyed(Player playerDestroyed)
        {

        }

        private void OnPlayerInstantiated(Player playerInstantiated)
        {

        }

        private void OnPlayerDisconnected(Player playerDisconnected)
        {
            Utils.Logger($"The player [{playerDisconnected.Nickname}] have disconnected from server :D");
        }
    }
}