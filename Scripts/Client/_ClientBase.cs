using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Client
{
    public class ClientBase : ClientBehaviour
    {
        //* Obtém a instância de Neutron, classe derivada.
        private Neutron This { get; set; }
        //* Obtém o tipo de cliente da instância.
        public global::Client ClientType { get; set; }

        //* Inicializa o cliente e registra os eventos de Neutron.
        protected void Initialize(global::Client client)
        {
            This = (Neutron)this;
            {
                ClientType = client;
                {
                    This.OnNeutronConnected += OnNeutronConnected;
                    This.OnPlayerConnected += OnPlayerConnected;
                    This.OnPlayerDisconnected += OnPlayerDisconnected;
                    This.OnMessageReceived += OnMessageReceived;
                    This.OnChannelsReceived += OnChannelsReceived;
                    This.OnRoomsReceived += OnRoomsReceived;
                    This.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
                    This.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
                    This.OnPlayerNicknameChanged += OnPlayerNicknameChanged;
                    This.OnPlayerCustomPacketReceived += OnPlayerCustomPacketReceived;
                    This.OnPlayerCreatedRoom += OnPlayerCreatedRoom;
                    This.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
                    This.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
                    This.OnPlayerLeftChannel += OnPlayerLeftChannel;
                    This.OnPlayerLeftRoom += OnPlayerLeftRoom;
                    This.OnFail += OnFail;
                }
            }
        }

        //* Usado para enviar os dados para o servidor.
        //* o pacote é usado apenas para definir se conta ou não os bytes enviados e recebidos.
        protected async void Send(NeutronWriter writer, Protocol protocol = Protocol.Tcp, Packet packet = Packet.Empty)
        {
            try
            {
                if (This.IsConnected)
                {
                    using (NeutronWriter headerWriter = Neutron.PooledNetworkWriters.Pull())
                    {
                        byte[] packetBuffer = writer.ToArray().Compress();
                        //* Cabeçalho da mensagem/dados.
                        #region Header
                        headerWriter.WriteSize(packetBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro(4 bytes), e a mensagem.
                        #endregion
                        byte[] headerBuffer = headerWriter.ToArray();
                        switch (protocol)
                        {
                            case Protocol.Tcp:
                                {
                                    NetworkStream netStream = TcpClient.GetStream();
                                    await netStream.WriteAsync(headerBuffer, 0, headerBuffer.Length); //* Envia os dados pro servidor.
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ClientTCP.AddOutgoing(packetBuffer.Length, packet);
#endif
                                }
                                break;
                            case Protocol.Udp:
                                {
                                    if ((NeutronModule.Settings.LagSimulationSettings.Drop && This.Player != null && This.Player.IsConnected) && !OthersHelper.Odds())
                                        return;

                                    await UdpClient.SendAsync(headerBuffer, headerBuffer.Length, _udpEndPoint); //* Envia os dados pro servidor.
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ClientUDP.AddOutgoing(packetBuffer.Length, packet);
#endif
                                }
                                break;
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }

        //* Executa o iRPC na instância especificada.
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        protected async void iRPCHandler(byte rpcId, short viewId, byte instanceId, byte[] buffer, NeutronPlayer player, RegisterType registerType)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            async Task Run((int, int, RegisterType) key)
            {
                if (MatchmakingHelper.GetNetworkObject(key, This.Player, out NeutronView neutronView))
                {
                    if (neutronView.iRPCs.TryGetValue((rpcId, instanceId), out RPC remoteProceduralCall))
                    {
                        try
                        {
                            await ReflectionHelper.iRPC(buffer, remoteProceduralCall, player);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.StackTrace(ex);
                        }
                    }
                }
            }

            switch (registerType)
            {
                case RegisterType.Scene:
                    await Run((0, viewId, registerType));
                    break;
                case RegisterType.Player:
                    await Run((viewId, viewId, registerType));
                    break;
                case RegisterType.Dynamic:
                    await Run((player.ID, viewId, registerType));
                    break;
            }
        }

        //* Executa o gRPC, chamada global, não é por instâncias.
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        protected async void gRPCHandler(int id, NeutronPlayer player, byte[] buffer, bool isServer, bool isMine)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPC remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                try
                {
                    await ReflectionHelper.gRPC(player, buffer, remoteProceduralCall, isServer, isMine, This);
                }
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
                }
            }
            else
                LogHelper.Error("Invalid gRPC ID, there is no attribute with this ID.");
        }

        // Executa o AutoSync.
        protected void OnAutoSyncHandler(NeutronPlayer player, short viewId, byte instanceId, byte[] buffer, RegisterType registerType)
        {
            void Run((int, int, RegisterType) key)
            {
                if (MatchmakingHelper.GetNetworkObject(key, This.Player, out NeutronView neutronView)) //* Obtém o objeto de rede com o ID especificado.
                {
                    if (neutronView.NeutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                    {
                        using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                        {
                            reader.SetBuffer(buffer);
                            neutronBehaviour.OnAutoSynchronization(null, reader, false);
                        }
                    }
                }
            }

            switch (registerType)
            {
                case RegisterType.Scene:
                    Run((0, viewId, registerType));
                    break;
                case RegisterType.Player:
                    Run((viewId, viewId, registerType));
                    break;
                case RegisterType.Dynamic:
                    Run((player.ID, viewId, registerType));
                    break;
            }
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação do seu jogador.<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreatePlayer(NeutronWriter writer, byte id)
        {
            This.gRPC(id, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação de um objeto.<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreateObject(NeutronWriter writer, byte id)
        {
            This.gRPC(id, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        /// </summary>
        /// <param name="player">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(NeutronPlayer player)
        {
            if (This.Player == null)
                return LogHelper.Error("It is not possible to send data before initialization of the main player.");
            return player.Equals(This.Player);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return This.Player.Matchmaking.Player.Equals(This.Player);
        }

        #region Events
        private void OnPlayerLeftRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron instance)
        {
            player.Room = null;
            player.Matchmaking = null;
        }

        private void OnPlayerLeftChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron neutron)
        {
            player.Channel = null;
            player.Matchmaking = null;
        }

        private void OnPlayerCustomPacketReceived(NeutronReader reader, NeutronPlayer player, CustomPacket packet, Neutron neutron)
        {

        }

        private void OnPlayerNicknameChanged(NeutronPlayer player, string nickname, bool isMine, Neutron neutron)
        {
            player.Nickname = nickname;
        }

        private void OnPlayerPropertiesChanged(NeutronPlayer player, bool isMine, Neutron neutron)
        {

        }

        private void OnRoomPropertiesChanged(NeutronPlayer nPlayer, bool isMine, Neutron instance)
        {

        }

        private void OnRoomsReceived(NeutronRoom[] rooms, Neutron instance)
        {

        }

        private void OnChannelsReceived(NeutronChannel[] nChannels, Neutron instance)
        {

        }

        private void OnPlayerDisconnected(string reason, NeutronPlayer player, bool isMine, Neutron instance)
        {

        }

        private void OnPlayerConnected(NeutronPlayer player, bool isMine, Neutron instance)
        {

        }

        private void OnPlayerJoinedChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron instance)
        {
            player.Channel = channel;
            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
        }

        private void OnPlayerJoinedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron instance)
        {
            player.Room = room;
            player.Matchmaking = MatchmakingHelper.Matchmaking(player);
        }

        private void OnPlayerCreatedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron instance)
        {

        }

        private void OnMessageReceived(String message, NeutronPlayer player, bool isMine, Neutron instance)
        {

        }

        private void OnNeutronConnected(bool success, Neutron instance)
        {
            NeutronSchedule.ScheduleTask(() =>
            {
                if (success && ClientType == global::Client.Player)
                {
#if !UNITY_SERVER
                    NeutronModule.Chronometer.Start();
#endif
                    SceneHelper.CreateContainer(OthersHelper.GetConstants().ContainerName, hasPhysics: Neutron.Server.ClientHasPhysics, physics: Neutron.Server.Physics);
                }
            });
        }

        private void OnFail(Packet packet, string message, Neutron instance)
        {
            LogHelper.Error($"[{packet}] -> | ERROR | {message}");
        }
        #endregion
    }
}