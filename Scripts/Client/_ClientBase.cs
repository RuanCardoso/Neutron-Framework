using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;
using System.Linq;
using System.Net.Sockets;
using UnityEngine;

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
                    This.OnNeutronConnected.Add(OnNeutronConnected);
                    This.OnPlayerConnected.Add(OnPlayerConnected);
                    This.OnPlayerDisconnected.Add(OnPlayerDisconnected);
                    This.OnChannelsReceived.Add(OnChannelsReceived);
                    This.OnRoomsReceived.Add(OnRoomsReceived);
                    This.OnRoomPropertiesChanged.Add(OnRoomPropertiesChanged);
                    This.OnPlayerPropertiesChanged.Add(OnPlayerPropertiesChanged);
                    This.OnPlayerNicknameChanged.Add(OnPlayerNicknameChanged);
                    This.OnPlayerPacketReceived.Add(OnPlayerPacketReceived);
                    This.OnPlayerLeftChannel.Add(OnPlayerLeftChannel);
                    This.OnPlayerLeftRoom.Add(OnPlayerLeftRoom);
                    This.OnPlayerDestroyed.Add(OnPlayerDestroyed);
                    This.OnPlayerInstantiated.Add(OnPlayerInstantiated);
                    This.OnFail.Add(OnFail);
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
                                NetworkStream netStream = TcpSocket.GetStream();
                                using (NeutronWriter header = Neutron.PooledNetworkWriters.Pull())
                                {
                                    byte[] buffer = writer.ToArray();
                                    #region Header
                                    header.SetLength(0); //* Limpa o escritor.
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
                                await UdpSocket.SendAsync(buffer, buffer.Length, _udpEndPoint); //* Envia os dados pro servidor.
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
        protected void iRPCHandler(int nIRPCId, int nPlayerId, byte[] nParameters, bool nIsMine, NeutronPlayer nSender)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            // if (NetworkObjects.TryGetValue(nPlayerId, out NeutronView nView)) //* Obtém o objeto de rede com o ID especificado.
            // {
            //     if (nView.iRPCs.TryGetValue(nIRPCId, out RPC remoteProceduralCall)) //* Obtém o iRPC com o ID especificado.
            //     {
            //         iRPC nIRPCAttr = (iRPC)remoteProceduralCall.attribute;
            //         if (nIRPCAttr != null)
            //         {
            //             Action _ = new Action(() => NeutronHelper.iRPC(nParameters, nIsMine, remoteProceduralCall, nSender, nView));
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
        protected void gRPCHandler(int id, NeutronPlayer player, byte[] buffer, bool isServer, bool isMine)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPC remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                gRPC gRPC = remoteProceduralCall.GRPC;
                if (gRPC != null)
                {
                    Action __ = new Action(() => PlayerHelper.gRPC(id, player, buffer, remoteProceduralCall, isServer, isMine, This));
                    {
                        if (gRPC.RunInMonoBehaviour)
                            NeutronDispatcher.Dispatch(__); //* Invoca o metódo na thread main(Unity).
                        else __.Invoke(); //* Invoca o metódo
                    }
                }
            }
        }

        protected void OnSerializeViewHandler(int viewId, int instanceId, byte[] buffer)
        {
            if (NetworkObjects.TryGetValue(viewId, out NeutronView nView)) //* Obtém o objeto de rede com o ID especificado.
            {
                if (nView.neutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour))
                {
                    nView.Dispatch(() =>
                    {
                        using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                        {
                            reader.SetBuffer(buffer);
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.SetLength(0);
                                neutronBehaviour.OnAutoSynchronization(writer, reader, false);
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação do seu jogador.<br/>
        ///* ID: 1001<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreatePlayer(NeutronWriter writer)
        {
            This.gRPC(This.Player.ID, NeutronConstants.CREATE_PLAYER, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação de um objeto.<br/>
        ///* ID: 1002<br/>
        /// </summary>
        /// <param name="writer">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreateObject(NeutronWriter writer)
        {
            This.gRPC(This.Player.ID, NeutronConstants.CREATE_OBJECT, writer, Protocol.Tcp);
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        /// </summary>
        /// <param name="player">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(NeutronPlayer player)
        {
            if (This.Player == null)
                return false;
            return player.Equals(This.Player);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return This.Room.Owner.Equals(This.Player);
        }

        /// <summary>
        ///* Registra os objetos da cena na rede, atribuindo-os um ID único.
        /// </summary>
        public void RegisterSceneObjects()
        {
            foreach (NeutronView nView in GameObject.FindObjectsOfType<NeutronView>().Where(x => x.IsSceneObject && !x.IsServer))
                NeutronRegister.RegisterSceneObject(This.Player, nView, false, This);
        }

        #region Callbacks
        private void OnPlayerInstantiated(NeutronPlayer nPlayer, GameObject nPrefab, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerDestroyed(Neutron nNeutron)
        {

        }

        private void OnPlayerLeftRoom(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerLeftChannel(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerPacketReceived(NeutronReader nReader, NeutronPlayer nPlayer, CustomPacket nClientPacket, Neutron nNeutron)
        {

        }

        private void OnPlayerNicknameChanged(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerPropertiesChanged(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnRoomPropertiesChanged(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnRoomsReceived(NeutronRoom[] nRooms, Neutron nNeutron)
        {

        }

        private void OnChannelsReceived(NeutronChannel[] nChannels, Neutron nNeutron)
        {

        }

        private void OnPlayerDisconnected(string nReason, NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerConnected(NeutronPlayer nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnNeutronConnected(bool nIsSuccess, Neutron nNeutron)
        {
            if (nIsSuccess && ClientType == global::Client.Player)
            {
#if !UNITY_SERVER
                NeutronMain.Chronometer.Start();
#endif
                SceneHelper.CreateContainer(NeutronConstants.CONTAINER_PLAYER_NAME);
            }
        }

        private void OnFail(Packet p1, string p2, Neutron p3)
        {

        }
        #endregion
    }
}