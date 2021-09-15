using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using System;
using System.Net.Sockets;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica ou não?.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Client
{
    public class ClientBase : ClientBehaviour
    {
        short _objectSpawnid;
        //* Define quando o cliente está pronto para uso.
        protected bool IsReady { get; set; }
        //* Obtém a instância de Neutron, classe derivada.
        protected Neutron This { get; set; }
        //* Mantém o estado do jogador.
        protected StateObject StateObject { get; set; }
        /// <summary>
        ///* Define se é a instância do servidor.
        /// </summary>
        public bool IsServer { get; set; }
        /// <summary>
        ///* Obtém o tipo de cliente da instância.
        /// </summary>
        public ClientMode ClientMode { get; set; }
        //* Inicializa o cliente e registra os eventos de Neutron.
        protected void Initialize(ClientMode clientMode)
        {
            This = (Neutron)this;
            {
                ClientMode = clientMode;
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
        //* o ignore é usado apenas para ignorar este pacote no profiler de banda(bandwidth), pacote interno que não deve ser contado.
        protected void Send(INeutronWriter header, INeutronWriter packet, Protocol protocol = Protocol.Tcp, Packet ignore = Packet.Empty)
        {
            try
            {
                if (IsServer)
                {
                    LogHelper.Error("To use this packet on the server side it is necessary to assign the \"Player\" parameter.");
                    return;
                }
#if !UNITY_SERVER
                if (This.IsConnected)
                {
                    byte[] packetBuffer = packet.ToArray().Compress(); //! Otimizado para evitar alocações, bom isso depende de como você usa o Neutron :p
                    switch (protocol)
                    {
                        //* ValueTask ainda não funciona na Unity, isso mesmo, em 2021 com .net 6 e standard 2.1, e a unity atrasada com essa merda de Mono, vê se pode?
                        case Protocol.Tcp:
                            {
                                if (header.GetPosition() == 0)
                                {
                                    header.WriteSize(packetBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro/short/byte(4/2/1 bytes), e a mensagem.
                                    byte[] headerBuffer = header.ToArray();
                                    if (header.IsFixedSize()) //* Verifica se o stream tem um tamanho fixo, é bom, evita alocações.
                                        header.EndWriteWithFixedCapacity(); //* finaliza a a escrita resetando a posição pra zero.
                                    else
                                        header.EndWrite(); //* finaliza a a escrita resetando a posição pra zero.

                                    NetworkStream networkStream = TcpClient.GetStream();
                                    switch (OthersHelper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            networkStream.Write(headerBuffer, 0, headerBuffer.Length); //* Envia os dados pro servidor de modo síncrono, esta opção é melhor, não aloca e tem performance de CPU.
                                            break;
                                        default:
                                            if (OthersHelper.GetConstants().SendAsyncPattern == AsynchronousType.APM)
                                                networkStream.Write(headerBuffer, 0, headerBuffer.Length); //* Envia os dados pro servidor de modo assíncrono, mentira, envia da mesma forma de "SendType.Synchronous", preguiça mesmo. :p, porque BeginReceive e Endreceive é chato de fazer pro TCP :D
                                            else
                                                SocketHelper.SendTcpAsync(networkStream, headerBuffer, TokenSource.Token); //* Envia os dados pro servidor de forma assíncrona., faz alocações pra caralho, no tcp não tanto, mas no UDP..... é foda. e usa muita cpu, evite, se souber como resolver, sinta-se a vontade para contribuir.
                                            break;
                                    }
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ClientTCP.AddOutgoing(packetBuffer.Length, ignore);
#endif
                                }
                                else
                                    LogHelper.Error($"Send: Invalid position, is not zero! Pos -> {header.GetPosition()} Capacity -> {header.GetCapacity()}");
                            }
                            break;
                        case Protocol.Udp:
                            {
                                if (UdpEndPoint == null)
                                    LogHelper.Error("Unauthenticated!");
                                else
                                {
                                    StateObject.SendDatagram = packetBuffer; //* O datagrama a ser usado para enviar os dados para a rede.
                                    switch (OthersHelper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            SocketHelper.SendBytes(UdpClient, packetBuffer, UdpEndPoint); //* envia de modo síncrono, evita alocações e performance boa.
                                            break;
                                        default:
                                            {
                                                switch (OthersHelper.GetConstants().SendAsyncPattern)
                                                {
                                                    case AsynchronousType.APM:
                                                        {
                                                            //* aloca, mas não tanto, boa performance.
                                                            SocketHelper.BeginSendBytes(UdpClient, packetBuffer, UdpEndPoint, (ar) =>
                                                            {
                                                                SocketHelper.EndSendBytes(UdpClient, ar);
                                                            }); //* Envia os dados pro servidor.
                                                            break;
                                                        }

                                                    default:
                                                        SocketHelper.SendUdpAsync(UdpClient, StateObject, UdpEndPoint); //* se foder, aloca pra caralho e usa cpu como a unreal engine, ValueTask poderia resolver, mas......
                                                        break;
                                                } //* se foder, aloca pra caralho e usa cpu como a unreal engine, ValueTask poderia resolver, mas......
                                                break;
                                            }
                                    }
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ClientUDP.AddOutgoing(packetBuffer.Length, ignore);
#endif
                                }
                            }
                            break;
                    }
                }
                else
                    LogHelper.Error("Non-connected socket, sending failed!");
#else
                    LogHelper.Error("To use this packet on the server side it is necessary to assign the \"Player\" parameter.");
#endif
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception ex)
            {
                LogHelper.StackTrace(ex);
            }
        }

        //* Usado para enviar os dados para o servidor.
        //* o ignoredPacket é usado apenas para definir se conta ou não os bytes enviados e recebidos.
        protected void Send(INeutronWriter writer, Protocol protocol = Protocol.Tcp, Packet ignore = Packet.Empty)
        {
            using (NeutronStream stream = new NeutronStream())
            {
                Send(stream.Writer, writer, protocol, ignore);
            }
        }

        //* Executa o iRPC na instância específicada.
#pragma warning disable IDE1006
        protected async void iRPCHandler(byte rpcId, short viewId, byte instanceId, byte[] parameters, NeutronPlayer player, RegisterMode registerMode)
#pragma warning restore IDE1006
        {
            async Task Run((int, int, RegisterMode) key) //* a key do objeto, o primeiro parâmetro é o ID do jogador ou do Objeto de Rede ou 0(se for objeto de cena), e o segundo é o ID do objeto, e o terceiro é o tipo de objeto.
            {
                if (MatchmakingHelper.Server.GetNetworkObject(key, This.Player, out NeutronView neutronView)) //* Obtém a instância que enviou o RPC para a rede.
                {
                    if (neutronView.iRPCs.TryGetValue((rpcId, instanceId), out RPCInvoker remoteProceduralCall)) //* Obtém o RPC com o ID enviado para a rede.
                    {
                        try
                        {
                            //* Executa o RPC, observe que isto não usa reflexão, reflexão é lento irmão, eu uso delegados, e a reflexão para criar os delegados em runtime no Awake do objeto, bem rápido, e funciona no IL2CPP.
                            await ReflectionHelper.iRPC(parameters, remoteProceduralCall, player);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.StackTrace(ex);
                        }
                    }
                }
            }

            switch (registerMode)
            {
                case RegisterMode.Scene:
                    await Run((0, viewId, registerMode));
                    break;
                case RegisterMode.Player:
                    await Run((viewId, viewId, registerMode));
                    break;
                case RegisterMode.Dynamic:
                    await Run((player.ID, viewId, registerMode));
                    break;
            }
        }

        //* Executa o gRPC, chamada global, não é por instâncias.
#pragma warning disable IDE1006
        protected async void gRPCHandler(int id, NeutronPlayer player, byte[] parameters, bool isServer, bool isMine)
#pragma warning restore IDE1006
        {
            if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPCInvoker remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                try
                {
                    //* Invoca os gRPC, não usa reflexão para invocar, utilizo delegados, reflexão usado para criar os delegados no awake, isto melhora a performance em 1000%, do que invocar usando a reflexão.
                    await ReflectionHelper.gRPC(player, parameters, remoteProceduralCall, isServer, isMine, This);
                }
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
                }
            }
            else
                LogHelper.Error("Invalid gRPC ID, there is no attribute with this ID.");
        }

        // Executa o AutoSync, observe que não usa reflexão e nem delegados, a invocação é direta, um metódo virtual, opte por usa isto sempre para sincronização constante, ex: movimentação.
        protected void OnAutoSyncHandler(NeutronPlayer player, short viewId, byte instanceId, byte[] buffer, RegisterMode registerType)
        {
            void Run((int, int, RegisterMode) key)
            {
                if (MatchmakingHelper.Server.GetNetworkObject(key, This.Player, out NeutronView neutronView)) //* Obtém o objeto de rede com o ID especificado.
                {
                    if (neutronView.NeutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour)) //* obtém a instância que está sincronizando os dados.
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
                case RegisterMode.Scene:
                    Run((0, viewId, registerType));
                    break;
                case RegisterMode.Player:
                    Run((viewId, viewId, registerType));
                    break;
                case RegisterMode.Dynamic:
                    Run((player.ID, viewId, registerType));
                    break;
            }
        }

        /// <summary>
        ///* Inicia uma chamada de preparação para a criação do seu jogador em rede. 
        /// </summary>
        /// <param name="parameters">* O escritor dos parâmetros.</param>
        /// <param name="position">* A posição inicial do spawn do objeto.</param>
        /// <param name="quaternion">* A rotação inicial do spawn do objeto.</param>
        public void BeginCreatePlayer(NeutronWriter parameters, Vector3 position, Quaternion quaternion)
        {
            parameters.Write(position);
            parameters.Write(quaternion);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que inicia a criação do seu jogador.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndCreatePlayer(NeutronWriter parameters, byte id)
        {
            This.gRPC(id, parameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Finaliza a chamada de criação do jogador em rede.<br/>
        /// </summary>
        public bool EndCreatePlayer(NeutronReader parameters, out Vector3 position, out Quaternion quaternion)
        {
            try
            {
                position = parameters.ReadVector3();
                quaternion = parameters.ReadQuaternion();
                return true;
            }
            catch
            {
                position = Vector3.zero;
                quaternion = Quaternion.identity;
                return false;
            }
        }

        /// <summary>
        ///* Inicia uma chamada de preparação para a criação de um objeto em rede. 
        /// </summary>
        /// <param name="parameters">* O escritor dos parâmetros.</param>
        /// <param name="position">* A posição inicial do spawn do objeto.</param>
        /// <param name="quaternion">* A rotação inicial do spawn do objeto.</param>
        public void BeginCreateObject(NeutronWriter parameters, Vector3 position, Quaternion quaternion)
        {
            parameters.Write(position);
            parameters.Write(quaternion);
            parameters.Write(++_objectSpawnid);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que inicia a criação de um objeto de rede.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndCreateObject(NeutronWriter parameters, byte id)
        {
            This.gRPC(id, parameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Finaliza a chamada de criação de um objeto de rede.<br/>
        /// </summary>
        public bool EndCreateObject(NeutronReader parameters, out Vector3 position, out Quaternion quaternion, out short spawnId)
        {
            try
            {
                position = parameters.ReadVector3();
                quaternion = parameters.ReadQuaternion();
                spawnId = parameters.ReadInt16();
                return true;
            }
            catch
            {
                position = Vector3.zero;
                quaternion = Quaternion.identity;
                spawnId = -1;
                return false;
            }
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        /// </summary>
        /// <param name="player">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(NeutronPlayer player)
        {
            return This.Player == null
                ? LogHelper.Error("It is not possible to send data before initialization of the main player.")
                : player.Equals(This.Player);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return This.Player == null
                ? LogHelper.Error("It is not possible to send data before initialization of the main player.")
                : This.Player.Matchmaking == null
                ? LogHelper.Error("It is not possible to send data before initialization of the matchmaking.")
                : This.Player.Matchmaking.Player.Equals(This.Player);
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
                if (success && ClientMode == ClientMode.Player)
                {
#if !UNITY_SERVER && !UNITY_NEUTRON_LAN
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