using NeutronNetwork.Components;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using NeutronNetwork.Server.Internal;
using System;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    ///* Eventos ao lado do servidor ou cliente, em sua maioria, eventos do servidor.
    /// </summary>
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_EVENTS)]
    public abstract class ServerSide : GlobalBehaviour
    {
        #region Properties
        /// <summary>
        ///* Define se a simulação de física deve ser no FixedUpdate.
        /// </summary>
        protected virtual bool SimulateOnFixedUpdate {
            get;
        }
        /// <summary>
        ///* Retorna se é o servidor, sempre falso no Editor.
        /// </summary>
#if UNITY_SERVER || UNITY_EDITOR || UNITY_NEUTRON_LAN
        protected bool IsServer { get; } = true;
#else
        protected bool IsServer { get; } = false;
#endif
        /// <summary>
        ///* Retorna a instância do servidor.
        /// </summary>
        protected Neutron Server {
            get;
            private set;
        }
        #endregion

        #region Fields
        private float _physicsTimer;
        #endregion

        #region MonoBehaviour
        protected virtual void OnEnable()
        {
            NeutronServer.OnStart += OnStart;
            NeutronServer.OnReceivePacket += OnReceivePacket;
            MatchmakingHelper.Internal.OnCustomTunneling += OnCustomBroadcast;
            MatchmakingHelper.Internal.OnCustomTarget += OnCustomTarget;
            ServerBase.OnAwake += OnServerAwake;
            ServerBase.OnMessageReceived += OnMessageReceived;
            ServerBase.OnPlayerCreatedRoom += OnPlayerCreatedRoom;
            ServerBase.OnPlayerDestroyed += OnPlayerDestroyed;
            ServerBase.OnPlayerConnected += OnPlayerConnected;
            ServerBase.OnPlayerDisconnected += OnPlayerDisconnected;
            ServerBase.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            ServerBase.OnPlayerJoinedRoom += Internal_OnPlayerJoinedRoom;
            ServerBase.OnPlayerLeftChannel += OnPlayerLeftChannel;
            ServerBase.OnPlayerLeftRoom += OnPlayerLeftRoom;
            ServerBase.OnPlayerNicknameChanged += OnPlayerNicknameChanged;
            ServerBase.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
            ServerBase.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
            ServerBase.OnAuthentication += OnAuthentication;
            PhysicsManager.OnPhysics += OnPhysics;
            PhysicsManager.OnPhysics2D += OnPhysics2D;
        }

        protected virtual void OnDisable()
        {
            NeutronServer.OnStart -= OnStart;
            NeutronServer.OnReceivePacket -= OnReceivePacket;
            MatchmakingHelper.Internal.OnCustomTunneling -= OnCustomBroadcast;
            MatchmakingHelper.Internal.OnCustomTarget -= OnCustomTarget;
            ServerBase.OnAwake -= OnServerAwake;
            ServerBase.OnMessageReceived -= OnMessageReceived;
            ServerBase.OnPlayerCreatedRoom -= OnPlayerCreatedRoom;
            ServerBase.OnPlayerDestroyed -= OnPlayerDestroyed;
            ServerBase.OnPlayerConnected -= OnPlayerConnected;
            ServerBase.OnPlayerDisconnected -= OnPlayerDisconnected;
            ServerBase.OnPlayerJoinedChannel -= OnPlayerJoinedChannel;
            ServerBase.OnPlayerJoinedRoom -= Internal_OnPlayerJoinedRoom;
            ServerBase.OnPlayerLeftChannel -= OnPlayerLeftChannel;
            ServerBase.OnPlayerLeftRoom -= OnPlayerLeftRoom;
            ServerBase.OnPlayerNicknameChanged -= OnPlayerNicknameChanged;
            ServerBase.OnPlayerPropertiesChanged -= OnPlayerPropertiesChanged;
            ServerBase.OnRoomPropertiesChanged -= OnRoomPropertiesChanged;
            ServerBase.OnAuthentication -= OnAuthentication;
            PhysicsManager.OnPhysics -= OnPhysics;
            PhysicsManager.OnPhysics2D -= OnPhysics2D;
        }

        protected virtual void OnServerAwake()
        {
            PhysicsManager.IsFixedUpdate = SimulateOnFixedUpdate;
        }

        protected virtual void OnStart()
        {
            Server = Neutron.Server.Instance;
            MakeServerContainer();
            MakeContainerOnChannels();
        }
        #endregion

        #region Registered Methods
        protected virtual void OnPhysics(PhysicsScene physicsScene)
        {
            if (!physicsScene.IsValid())
                return; // do nothing if the physics Scene is not valid.

            _physicsTimer += Time.deltaTime;

            // Catch up with the game time.
            // Advance the physics simulation in portions of Time.fixedDeltaTime
            // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
            while (_physicsTimer >= Time.fixedDeltaTime)
            {
                _physicsTimer -= Time.fixedDeltaTime;
                physicsScene.Simulate(Time.fixedDeltaTime);
            }

            // Here you can access the transforms state right after the simulation, if needed...
        }

        protected virtual void OnPhysics2D(PhysicsScene2D physicsScene)
        {
            if (!physicsScene.IsValid())
                return; // do nothing if the physics Scene is not valid.

            _physicsTimer += Time.deltaTime;

            // Catch up with the game time.
            // Advance the physics simulation in portions of Time.fixedDeltaTime
            // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
            while (_physicsTimer >= Time.fixedDeltaTime)
            {
                _physicsTimer -= Time.fixedDeltaTime;
                physicsScene.Simulate(Time.fixedDeltaTime);
            }

            // Here you can access the transforms state right after the simulation, if needed...
        }

        private async void Internal_OnPlayerJoinedRoom(NeutronPlayer player, NeutronRoom room)
        {
            await NeutronSchedule.ScheduleTaskAsync(() =>
            {
                MakeRoomContainer(room);
            });
            OnPlayerJoinedRoom(player, room);
        }

        protected virtual bool OnReceivePacket(Packet packet)
        {
            return true;
        }

        protected virtual async Task<bool> OnAuthentication(NeutronPlayer player, Authentication authentication)
        {
            return await Task.Run(() => true);
        }

#pragma warning disable UNT0006
        protected virtual void OnPlayerConnected(NeutronPlayer player)
#pragma warning restore UNT0006
        {

        }

#pragma warning disable UNT0006
        protected virtual void OnPlayerDisconnected(NeutronPlayer player)
#pragma warning restore UNT0006
        {
            LogHelper.Info($"The Player [{player.StateObject.TcpRemoteEndPoint}] Has been disconnected from server.");
        }

        protected virtual void OnCheatDetected(NeutronPlayer player, string name)
        {
            LogHelper.Info($"Usando hack porraaaaaaaa -> {player.Nickname}");
        }

        protected virtual bool OnPlayerCreatedRoom(NeutronPlayer player, NeutronRoom room)
        {
            return true;
        }

        protected virtual bool OnRoomPropertiesChanged(NeutronPlayer player, string properties)
        {
            return true;
        }

        protected virtual bool OnPlayerPropertiesChanged(NeutronPlayer player, string properties)
        {
            return true;
        }

        protected virtual bool OnPlayerNicknameChanged(NeutronPlayer player, string nickname)
        {
            return true;
        }

        protected virtual void OnPlayerLeftRoom(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerJoinedRoom(NeutronPlayer player, NeutronRoom room)
        {

        }

        protected virtual void OnPlayerLeftChannel(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerJoinedChannel(NeutronPlayer player)
        {

        }

        protected virtual void OnPlayerDestroyed(NeutronPlayer player)
        {

        }

        protected virtual bool OnMessageReceived(NeutronPlayer player, string message)
        {
            return true;
        }

        protected virtual NeutronPlayer[] OnCustomBroadcast(NeutronPlayer player, TunnelingTo tunnelingTo)
        {
            switch (tunnelingTo)
            {
                default:
                    LogHelper.Error("Broadcast Packet not implemented! " + tunnelingTo);
                    return null;
            }
        }

        protected virtual void OnCustomTarget(NeutronPlayer player, NeutronPacket packet, TargetTo targetTo, NeutronPlayer[] players)
        {
            throw new NotImplementedException("Ué cara???");
        }
        #endregion

        #region Internal
        private void MakeRoomContainer(NeutronRoom room)
        {
            if (room.PhysicsManager == null)
            {
                //* Cria um container(scene) para a nova sala, somente se não existir um.
                room.PhysicsManager = SceneHelper.CreateContainer($"[Container] -> Room[{room.Id}]", Neutron.Server.LocalPhysicsMode);
                GameObject roomManager = SceneHelper.MakeMatchmakingManager(room.Owner, IsServer, Server);
                SceneHelper.MoveToContainer(roomManager, $"[Container] -> Room[{room.Id}]");
                //* Registra os objetos de cena.
                SceneObject.OnSceneObjectRegister(room.Owner, IsServer, room.PhysicsManager.Scene, MatchmakingMode.Room, room, Server);
            }
        }

        private void MakeServerContainer() => SceneHelper.CreateContainer($"[Container] -> Server");

        private void MakeContainerOnChannels()
        {
            foreach (var channel in Neutron.Server.ChannelsById.Values)
            {
                MakeVirtualOwner(channel, channel, null);
                channel.PhysicsManager = SceneHelper.CreateContainer($"[Container] -> Channel[{channel.Id}]", Neutron.Server.LocalPhysicsMode);
                if (Neutron.Server.ActionsOnTheChannel)
                {
                    GameObject matchManager = SceneHelper.MakeMatchmakingManager(channel.Owner, true, Neutron.Server.Instance);
                    SceneHelper.MoveToContainer(matchManager, $"[Container] -> Channel[{channel.Id}]");
                    //* Registra os objetos de cena.
                    SceneObject.OnSceneObjectRegister(channel.Owner, IsServer, channel.PhysicsManager.Scene, MatchmakingMode.Channel, channel, Server);
                }
                MakeContainerOnRooms(channel);
            }
        }

        private void MakeContainerOnRooms(NeutronChannel channel)
        {
            foreach (NeutronRoom room in channel.GetRooms())
            {
                MakeVirtualOwner(room, channel, room);
                MakeRoomContainer(room);
            }
        }

        private void MakeVirtualOwner(INeutronMatchmaking matchmaking, NeutronChannel channel, NeutronRoom room)
        {
            if (matchmaking.Owner != null)
                LogHelper.Error("Matchmaking already has an owner!");
            else
            {
                //* Não pode aproveitar o Neutron.Server.Player? não, não podemos compartilhar a mesma instância pra vários matchmaking, um jogador só pode está em um Matchmaking ao mesmo tempo.
                NeutronPlayer player = PlayerHelper.MakeTheServerPlayer();
                player.Channel = channel;
                player.Room = room;
                player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                matchmaking.Owner = player; //! reforço: um jogador só pode está em um Matchmaking ao mesmo tempo.
            }
        }

        /// <summary>
        ///* Envia o estado da autenticação ao seu usuário.
        /// </summary>
        /// <param name="user">* O usuário a ser autenticado.</param>
        /// <param name="authStatus">* O estado da autenticação</param>
        /// <returns></returns>
        protected bool OnAuth(NeutronPlayer user, bool authStatus)
        {
            user.Properties = string.IsNullOrEmpty(user.Properties) ? "{\"Neutron\":\"Neutron\"}" : user.Properties;
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IWriter writer = stream.Writer;
                writer.WritePacket((byte)Packet.AuthStatus);
                writer.Write(user.Properties);
                writer.Write(authStatus);
                user.Write(writer);
            }
            return authStatus;
        }
        #endregion
    }
}