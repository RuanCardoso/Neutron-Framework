using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;
using System.Linq;
using System.Net.Sockets;
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
        private Neutron This;
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
        protected async void Send(NeutronWriter writer, Protocol protocol = Protocol.Tcp)
        {
            try
            {
                if (This.IsConnected)
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                NetworkStream netStream = TcpClient.GetStream();
                                using (NeutronWriter header = Neutron.PooledNetworkWriters.Pull())
                                {
                                    byte[] buffer = writer.ToArray();
                                    #region Header
                                    header.WriteFixedLength(buffer.Length); //* Pre-fixa o tamanho da mensagem no buffer.
                                    header.Write(buffer); //* Os dados enviados junto do pre-fixo.
                                    #endregion

                                    byte[] hBuffer = header.ToArray();
                                    await netStream.WriteAsync(hBuffer, 0, hBuffer.Length); //* Envia os dados pro servidor.
                                    {
                                        //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                        NeutronStatistics.m_ClientTCP.AddOutgoing(buffer.Length);
                                    }
                                }
                            }
                            break;
                        case Protocol.Udp:
                            {
                                if ((NeutronMain.Settings.LagSimulationSettings.Drop && This.Player != null && This.Player.IsConnected) && !OthersHelper.Odds())
                                    return;

                                byte[] buffer = writer.ToArray();
                                await UdpClient.SendAsync(buffer, buffer.Length, _udpEndPoint); //* Envia os dados pro servidor.
                                {
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ClientUDP.AddOutgoing(buffer.Length);
                                }
                            }
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception) { }
        }

        //* Executa o iRPC na instância especificada.
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        protected void iRPCHandler(int nIRPCId, int nPlayerId, byte[] nParameters, bool isMine, NeutronPlayer nSender)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            // if (NetworkObjects.TryGetValue(nPlayerId, out NeutronView nView)) //* Obtém o objeto de rede com o ID especificado.
            // {
            //     if (nView.iRPCs.TryGetValue(nIRPCId, out RPC remoteProceduralCall)) //* Obtém o iRPC com o ID especificado.
            //     {
            //         iRPC nIRPCAttr = (iRPC)remoteProceduralCall.attribute;
            //         if (nIRPCAttr != null)
            //         {
            //             Action _ = new Action(() => NeutronHelper.iRPC(nParameters, isMine, remoteProceduralCall, nSender, nView));
            //             {
            //                 if (nIRPCAttr.DispatchOnMainThread)
            //                     NeutronDispatcher.Dispatch(_); //* Invoca o metódo na thread main(Unity).
            //                 else _.Invoke(); //* Invoca o metódo
            //             }
            //         }
            //     }
            //     else LogHelper.LoggerError("Invalid iRPC ID, there is no attribute with this ID in the target object.");
            // }
        }

        //* Executa o gRPC, chamada global, não é por instâncias.
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        protected async void gRPCHandler(int id, NeutronPlayer player, byte[] buffer, bool isServer, bool isMine)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPC remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                gRPC gRPC = remoteProceduralCall.GRPC;
                if (gRPC != null)
                {
                    //Action __ = new Action(() => PlayerHelper.gRPC(id, player, buffer, remoteProceduralCall, isServer, isMine, This));
                    {
                        //if (gRPC.RunInMonoBehaviour) { }
                        ////NeutronDispatcher.Dispatch(__); //* Invoca o metódo na thread main(Unity).
                        //else __.Invoke(); //* Invoca o metódo
                        try
                        {
                            await PlayerHelper.gRPC(id, player, buffer, remoteProceduralCall, isServer, isMine, This);
                        }
                        catch (Exception ex) { LogHelper.StackTrace(ex); }
                    }
                }
            }
        }

        // Executa o AutoSync.
        protected void OnAutoSyncHandler(NeutronPlayer player, int viewId, int instanceId, byte[] buffer, RegisterType registerType)
        {
            switch (registerType)
            {
                case RegisterType.Scene:
                    {
                        if (MatchmakingHelper.GetNetworkObject((0, viewId), This.Player, out NeutronView neutronView)) //* Obtém o objeto de rede com o ID especificado.
                        {
                            if (neutronView.Childs.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                            {
                                using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    reader.SetBuffer(buffer);
                                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                                    {
                                        neutronBehaviour.OnAutoSynchronization(writer, reader, false);
                                    }
                                }
                            }
                        }
                        else
                            Debug.LogError("view not found");
                    }
                    break;
                case RegisterType.Player:
                    {
                        if (MatchmakingHelper.GetNetworkObject((viewId, viewId), This.Player, out NeutronView neutronView))
                        {
                            if (neutronView.Childs.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                            {
                                using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    reader.SetBuffer(buffer);
                                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                                    {
                                        neutronBehaviour.OnAutoSynchronization(writer, reader, false);
                                    }
                                }
                            }
                        }
                        else
                            Debug.LogError("view not found");
                    }
                    break;
                case RegisterType.Dynamic:
                    {
                        if (MatchmakingHelper.GetNetworkObject((player.ID, viewId), This.Player, out NeutronView neutronView))
                        {
                            if (neutronView.Childs.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                            {
                                using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    reader.SetBuffer(buffer);
                                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                                    {
                                        neutronBehaviour.OnAutoSynchronization(writer, reader, false);
                                    }
                                }
                            }
                        }
                        else
                            Debug.LogError("view not found");
                    }
                    break;
            }
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação do seu jogador.<br/>
        ///* ID: 1001<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreatePlayer(NeutronWriter writer)
        {
            This.gRPC(This.Player.ID, Settings.CREATE_PLAYER, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação de um objeto.<br/>
        ///* ID: 1002<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreateObject(NeutronWriter writer)
        {
            This.gRPC(This.Player.ID, Settings.CREATE_OBJECT, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        /// </summary>
        /// <param name="player">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(NeutronPlayer player)
        {
            return player.Equals(This.Player);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return This.Player.Room.Owner.Equals(This.Player);
        }

        /// <summary>
        ///* Registra os objetos da cena na rede, atribuindo-os um ID único.
        /// </summary>
        public void RegisterSceneObjects()
        {
            foreach (NeutronView view in GameObject.FindObjectsOfType<NeutronView>().Where(x => x.IsSceneObject && !x.IsServer))
                view.OnNeutronRegister(This.Player, false, RegisterType.Scene, This);
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
                    NeutronMain.Chronometer.Start();
#endif
                    SceneHelper.CreateContainer(OthersHelper.GetSettings().CONTAINER_PLAYER_NAME);
                }
            });
        }

        private void OnFail(Packet packet, string message, Neutron instance)
        {

        }
        #endregion
    }
}