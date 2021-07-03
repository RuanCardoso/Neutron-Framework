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
    public class NeutronClientFunctions : NeutronClientConstants
    {
        //* Obtém a instância de Neutron, classe derivada.
        private Neutron _;
        //* Inicializa o cliente e registra os eventos de Neutron.
        protected void InitializeClient(ClientType nClientType)
        {
            _ = (Neutron)this;

            #region Events
            Neutron.OnNeutronConnected.Register(OnNeutronConnected);
            Neutron.OnPlayerConnected.Register(OnPlayerConnected);
            Neutron.OnPlayerDisconnected.Register(OnPlayerDisconnected);
            Neutron.OnChannelsReceived.Register(OnChannelsReceived);
            Neutron.OnRoomsReceived.Register(OnRoomsReceived);
            Neutron.OnRoomPropertiesChanged.Register(OnRoomPropertiesChanged);
            Neutron.OnPlayerPropertiesChanged.Register(OnPlayerPropertiesChanged);
            Neutron.OnPlayerNicknameChanged.Register(OnPlayerNicknameChanged);
            Neutron.OnPlayerPacketReceived.Register(OnPlayerPacketReceived);
            Neutron.OnPlayerLeftChannel.Register(OnPlayerLeftChannel);
            Neutron.OnPlayerLeftRoom.Register(OnPlayerLeftRoom);
            Neutron.OnPlayerDestroyed.Register(OnPlayerDestroyed);
            Neutron.OnPlayerInstantiated.Register(OnPlayerInstantiated);
            Neutron.OnFail.Register(OnFail);
            #endregion
        }

        //* Usado para enviar os dados para o servidor.
        protected async void Send(NeutronWriter nWriter, Protocol nProtocol = Protocol.Tcp)
        {
            try
            {
                if (_.IsConnected)
                {
                    switch (nProtocol)
                    {
                        case Protocol.Tcp:
                            {
                                NetworkStream nNetStream = TcpSocket.GetStream();
                                using (NeutronWriter nHeaderWriter = Neutron.PooledNetworkWriters.Pull())
                                {
                                    byte[] nBuffer = nWriter.ToArray();
                                    #region Header
                                    nHeaderWriter.SetLength(0); //* Limpa o escritor.
                                    nHeaderWriter.WriteFixedLength(nBuffer.Length); //* Pre-fixa o tamanho da mensagem no buffer.
                                    nHeaderWriter.Write(nBuffer); //* Os dados enviados junto do pre-fixo.
                                    #endregion

                                    byte[] nHeaderBuffer = nHeaderWriter.ToArray();
                                    await nNetStream.WriteAsync(nHeaderBuffer, 0, nHeaderBuffer.Length); //* Envia os dados pro servidor.
                                    {
                                        //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                        NeutronStatistics.m_ClientTCP.AddOutgoing(nBuffer.Length);
                                    }
                                }
                            }
                            break;
                        case Protocol.Udp:
                            {
                                byte[] nBuffer = nWriter.ToArray();
                                await UdpSocket.SendAsync(nBuffer, nBuffer.Length, UDPEndPoint); //* Envia os dados pro servidor.
                                {
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ClientUDP.AddOutgoing(nBuffer.Length);
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
        protected void iRPCHandler(int nIRPCId, int nPlayerId, byte[] nParameters, bool nIsMine, Player nSender)
        {
            if (NetworkObjects.TryGetValue(nPlayerId, out NeutronView nView)) //* Obtém o objeto de rede com o ID especificado.
            {
                if (nView.iRPCs.TryGetValue(nIRPCId, out RemoteProceduralCall remoteProceduralCall)) //* Obtém o iRPC com o ID especificado.
                {
                    iRPC nIRPCAttr = (iRPC)remoteProceduralCall.attribute;
                    if (nIRPCAttr != null)
                    {
                        Action _ = new Action(() => NeutronHelper.iRPC(nParameters, nIsMine, remoteProceduralCall, nSender, nView));
                        {
                            if (nIRPCAttr.DispatchOnMainThread)
                                NeutronDispatcher.Dispatch(_); //* Invoca o metódo na thread main(Unity).
                            else _.Invoke(); //* Invoca o metódo
                        }
                    }
                }
                else NeutronLogger.LoggerError("Invalid iRPC ID, there is no attribute with this ID in the target object.");
            }
        }

        //* Executa o gRPC, chamada global, não é por instâncias.
        protected void gRPCHandler(int nSRPCId, Player nSender, byte[] nParameters, bool nIsServer, bool nIsMine)
        {
            if (NeutronNonDynamicBehaviour.gRPCs.TryGetValue(nSRPCId, out RemoteProceduralCall remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                gRPC nSRPCAttr = (gRPC)remoteProceduralCall.attribute;
                if (nSRPCAttr != null)
                {
                    Action __ = new Action(() => NeutronHelper.gRPC(nSRPCId, nSender, nParameters, remoteProceduralCall, nIsServer, nIsMine, _));
                    {
                        if (nSRPCAttr.DispatchOnMainThread)
                            NeutronDispatcher.Dispatch(__); //* Invoca o metódo na thread main(Unity).
                        else __.Invoke(); //* Invoca o metódo
                    }
                }
            }
        }

        protected void OnSerializeViewHandler(int networkID, int instanceID, byte[] parameters)
        {
            if (NetworkObjects.TryGetValue(networkID, out NeutronView nView)) //* Obtém o objeto de rede com o ID especificado.
            {
                if (nView.NBs.TryGetValue(instanceID, out NeutronBehaviour neutronBehaviour))
                {
                    NeutronDispatcher.Dispatch(() =>
                    {
                        using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                        {
                            nReader.SetBuffer(parameters);
                            using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                            {
                                nWriter.SetLength(0);
                                {
                                    neutronBehaviour.OnNeutronSerializeView(nWriter, nReader, false);
                                }
                            }
                        }
                    });
                }
            }
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação do seu jogador.<br/>
        ///* ID: 1001<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="nParameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreatePlayer(NeutronWriter nParameters)
        {
            _.gRPC(_.MyPlayer.ID, 1001, nParameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que aciona a criação de um objeto.<br/>
        ///* ID: 1002<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="nParameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreateObject(NeutronWriter nParameters)
        {
            _.gRPC(_.MyPlayer.ID, 1002, nParameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="nPlayer">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(Player nPlayer)
        {
            return nPlayer.Equals(_.MyPlayer);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return _.CurrentRoom.Owner.Equals(_.MyPlayer);
        }

        /// <summary>
        ///* Registra os objetos da cena na rede, atribuindo-os um ID único.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        public void RegisterSceneObjects()
        {
            foreach (NeutronView nView in GameObject.FindObjectsOfType<NeutronView>().Where(x => x.IsSceneObject && !x.IsServer))
                NeutronRegister.RegisterSceneObject(_.MyPlayer, nView, false, _);
        }

        #region Callbacks
        private void OnPlayerInstantiated(Player nPlayer, GameObject nPrefab, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerDestroyed(Neutron nNeutron)
        {

        }

        private void OnPlayerLeftRoom(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerLeftChannel(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerPacketReceived(NeutronReader nReader, Player nPlayer, ClientPacket nClientPacket, Neutron nNeutron)
        {

        }

        private void OnPlayerNicknameChanged(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerPropertiesChanged(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnRoomPropertiesChanged(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnRoomsReceived(Room[] nRooms, Neutron nNeutron)
        {

        }

        private void OnChannelsReceived(Channel[] nChannels, Neutron nNeutron)
        {

        }

        private void OnPlayerDisconnected(string nReason, Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnPlayerConnected(Player nPlayer, bool nIsMine, Neutron nNeutron)
        {

        }

        private void OnNeutronConnected(bool nIsSuccess, Neutron nNeutron)
        {
            if (nIsSuccess)
                SceneHelper.CreateContainer(NeutronConstants.CONTAINER_PLAYER_NAME);
        }

        private void OnFail(SystemPacket p1, string p2, Neutron p3)
        {

        }
        #endregion
    }
}