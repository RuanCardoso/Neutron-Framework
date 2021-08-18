using NeutronNetwork.Client;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server;
using Newtonsoft.Json;
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork
{
    ///* Esta classe é o núcleo do Neutron, aqui é o lado do cliente, você pode fazer oque quiser.
    ///* Um Salve pra Unity Brasil.
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_CLIENT)]
    public class Neutron : ClientBase
    {
        #region Collections
        /// <summary>
        ///* Providencia um pool de escritores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronWriter> PooledNetworkWriters { get; set; }
        /// <summary>
        ///* Providencia um pool de leitores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronReader> PooledNetworkReaders { get; set; }
        /// <summary>
        ///* Providencia um pool de leitores e escritores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronStream> PooledNetworkStreams { get; set; }
        public static NeutronPool<NeutronPacket> PooledNetworkPackets { get; set; }
        //* Usado para retirar a média do RTT;
        private readonly List<double> _rtts = new List<double>();
        #endregion

        #region Fields -> Primitives
        //* Quantidade de pacotes perdidos.
        private int _pingAmountReceived;
        //* Quantidade de tentativas de ping.
        private int _pingAmount;
        //* Address de conexão.
        private string _host;
        //* Define quando o cliente está pronto para uso.
        private bool _isInitialized;
        #endregion

        #region Properties -> Static
        /// <summary>
        ///* Retorna a instância do servidor.<br/>
        ///* Esta propriedade só pode ser usada dentro dos pré-processadores #if UNITY_EDITOR || UNITY_SERVER<br/>
        ///! Só pode ser obtido no lado do servidor ou no Unity Editor, no cliente será nulo.
        /// </summary>
        public static NeutronServer Server {
            get {
#if !UNITY_EDITOR && !UNITY_SERVER
                if (ServerBase.This == null)
                    LogHelper.Error("You cannot access the server's methods and properties on the client, except within the Unity Editor.");
#endif
                return ServerBase.This;
            }
        }
        /// <summary>
        ///* Retorna a instância principal do Cliente.
        /// </summary>
        public static Neutron Client { get; set; }
        /// <summary>
        ///* Tempo atual do cliente/servidor, o tempo entre servidor e o cliente é sincronizado.
        /// </summary>
        public static double Time { get; set; }
        /// <summary>
        ///* A diferença de tempo entre o cliente e o servidor na ultima sincronização.<br/>
        ///! Não use, compensado automaticamente.
        /// </summary>
        public static double DiffTime { get; set; }
        #endregion

        #region Properties -> Instance
        /// <summary>
        ///* Retorna o objeto que representa seu jogador.
        /// </summary>
        public NeutronPlayer Player { get; set; }
        /// <summary>
        ///* Retorna a duração em milissegundos (ms) que uma solicitação de rede leva para ir de um ponto de partida a um destino e voltar.
        /// </summary>
        /// <value></value>
        public double RoundTripTime { get; set; }
        /// <summary>
        ///* Retorna a quantidade de pacotes que falharam em porcentagem(%).
        /// </summary>
        /// <value></value>
        public double PacketLoss { get; set; }
        /// <summary>
        ///* Retorna o status da sua conexão.
        /// </summary>
        public bool IsConnected { get; set; }
        /// <summary>
        ///* Obtém o nickname do seu jogador.
        /// </summary>
        public string Nickname { get; private set; }
        #endregion

        #region Properties -> Events
        /// <summary>
        ///* Este evento é acionado quando uma tentativa de conexão retorna seu estado.<br/>
        /// </summary>
        public NeutronEventNoReturn<bool, Neutron> OnNeutronConnected { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador se conecta ao servidor.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronPlayer, bool, Neutron> OnPlayerConnected { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador se desconecta do servidor.<br/>
        /// </summary>
        public NeutronEventNoReturn<string, NeutronPlayer, bool, Neutron> OnPlayerDisconnected { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador envia uma mensagem e esta mensagem é recebida.<br/>
        /// </summary>
        public NeutronEventNoReturn<string, NeutronPlayer, bool, Neutron> OnMessageReceived { get; set; }
        /// <summary>
        ///* Este evento é acionado quando a lista de canais disponíveis é recebida ou atualizada.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronChannel[], Neutron> OnChannelsReceived { get; set; }
        /// <summary>
        ///* Este evento é acionado quando a lista de salas disponíveis é recebida ou atualizada.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronRoom[], Neutron> OnRoomsReceived { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador sai do canal.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Neutron> OnPlayerLeftChannel { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador sai da sala.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerLeftRoom { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador cria uma sala.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerCreatedRoom { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador entra na sala.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Neutron> OnPlayerJoinedRoom { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador entra no canal.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Neutron> OnPlayerJoinedChannel { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador troca seu nickname.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronPlayer, string, bool, Neutron> OnPlayerNicknameChanged { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador atualiza suas propriedades.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronPlayer, bool, Neutron> OnPlayerPropertiesChanged { get; set; }
        /// <summary>
        ///* Este evento é acionado quando alguma sala atualiza suas propriedades.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronPlayer, bool, Neutron> OnRoomPropertiesChanged { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum jogador envia um pacote personalizado.<br/>
        /// </summary>
        public NeutronEventNoReturn<NeutronReader, NeutronPlayer, CustomPacket, Neutron> OnPlayerCustomPacketReceived { get; set; }
        /// <summary>
        ///* Este evento é acionado quando algum pacote apresenta uma falha.<br/>
        /// </summary>
        public NeutronEventNoReturn<Packet, string, Neutron> OnFail { get; set; }
        #endregion

        #region Yields
        private WaitForSeconds _yieldPing;
        private WaitForSeconds _yieldTcpKeepAlive;
        #endregion

        #region Methods -> Instance
        /// <summary>
        ///* Inicia uma tentativa de estabelecer uma conexão com o servidor.
        /// </summary>
        /// <param name="index">* Index do endereço de ip da lista de configurações</param>
        /// <param name="timeout">* Tempo limite de tentativa de estabelecer uma conexão.</param>
        /// <returns></returns>
        public async void Connect(int index = 0, int timeout = 3)
        {
#if UNITY_EDITOR
            await Task.Delay(40); //* Eu dei prioridade de execução pro servidor, ele inicializa primeiro que geral, mas coloquei esse delay por segurança, vai que o cliente inicia primeiro que o servidor, disponível somente no editor.
#endif
#if UNITY_SERVER
            if (ClientMode == ClientMode.Player)
                LogHelper.Info($"The main player has been removed from the server build, but you can choose to use a virtual player!\r\n");
            return;
#endif
            Initialize(); //* Inicializa o cliente.
            TcpClient.NoDelay = OthersHelper.GetSettings().GlobalSettings.NoDelay; //* Define um valor que desabilita o atraso ao enviar ou receber buffers que não estão cheios.
            TcpClient.ReceiveBufferSize = OthersHelper.GetConstants().TcpReceiveBufferSize;
            TcpClient.SendBufferSize = OthersHelper.GetConstants().TcpSendBufferSize;
            UdpClient.Client.ReceiveBufferSize = OthersHelper.GetConstants().UdpReceiveBufferSize;
            UdpClient.Client.SendBufferSize = OthersHelper.GetConstants().UdpSendBufferSize;

            //* Inicializa os temporizadores
            _yieldPing = new WaitForSeconds(NeutronModule.Settings.ClientSettings.PingRate); //* evita alocações, performance é prioridade.
            _yieldTcpKeepAlive = new WaitForSeconds(NeutronModule.Settings.ClientSettings.TcpKeepAlive); //* evita alocações, performance é prioridade.

            //* Obtém o ip do URL setado nas configurações.
            #region Host Resolver
            _host = NeutronModule.Settings.GlobalSettings.Addresses[index];
            int port = NeutronModule.Settings.GlobalSettings.Port;
            if (!string.IsNullOrEmpty(_host))
            {
                if (!IPAddress.TryParse(_host, out IPAddress _))
                {
                    if (!_host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
                    {
                        _host = _host.Replace("http://", string.Empty);
                        _host = _host.Replace("https://", string.Empty);
                        _host = _host.Replace("/", string.Empty);
                        _host = (await SocketHelper.GetHostAddress(_host)).ToString();
                    }
                    else
                        _host = IPAddress.Loopback.ToString();
                }
                else
                { /*Continue*/ }
            }
            else
                _host = IPAddress.Loopback.ToString();
            #endregion

            if (!IsConnected)
            {
                bool result = await TcpClient.ConnectAsync(_host, port).RunWithTimeout(new TimeSpan(0, 0, timeout));
                if (!result)
                {
                    OnNeutronConnected?.Invoke(IsConnected, this);
                    if (!LogHelper.Error("An attempt to establish a connection to the server failed."))
                        Dispose();
                }
                else if (result)
                {
                    IsConnected = TcpClient.Connected; //* define o estado da conexão.
                    //* Invoca o evento de conexão, com o estado da conexão.
                    OnNeutronConnected?.Invoke(IsConnected, this);
                    // Obtém o tipo de stream definido nas configurações(networkStream or bufferedStream). bufferedStream aumenta a performance em 10x.
                    Stream networkStream = SocketHelper.GetStream(TcpClient);
                    //* Dedica um thread a receber os dados do servidor, este é o unico thread que vai receber os dados, ele não vai processar os pacotes, apenas receber.
                    //* Terá outro thread, dedicado a processar os pacotes, fiz assim, pra evitar usar sincronização(lock) que é chato para caralho, sfd, por que os dados sempre são recebidos em threads diferentes, o famoso, IOCP Threads do sistema operacional windows ou epoll do Linux.
                    switch (OthersHelper.GetConstants().ReceiveThread)
                    {
                        case ThreadType.Neutron:
                            {
                                ThreadPool.QueueUserWorkItem((e) =>
                                {
                                    OnReceivingData(networkStream, Protocol.Tcp); //* Inicia a leitura dos dados Tcp.
                                    OnReceivingData(networkStream, Protocol.Udp); //* Inicia a leitura dos dados Udp.
                                });
                                break;
                            }
                        case ThreadType.Unity:
                            OnReceivingData(networkStream, Protocol.Tcp); //* Inicia a leitura dos dados Tcp.
                            OnReceivingData(networkStream, Protocol.Udp); //* Inicia a leitura dos dados Udp.
                            break;
                    }

                    NeutronSchedule.Schedule.StartCoroutine(Ping());
                    NeutronSchedule.Schedule.StartCoroutine(TcpKeepAlive()); //* pacote de manunteção pra manter o TCP ativo.
                }
                //* Envia um pacote de reconhecimento pro servidor e ele responde com suas informações.
                #region Handshake
                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                {
                    string appId = OthersHelper.GetSettings().GlobalSettings.AppId;
                    writer.WritePacket((byte)Packet.Handshake);
                    writer.Write(appId.Encrypt());
                    writer.Write(Time);
                    Send(writer);
                }
                #endregion
            }
            else
                LogHelper.Error("Connection Refused!");
        }

        //* Recebe a porra dos dados do socket udp e monta o pacote.
        private void CreateUdpPacket()
        {
            using (NeutronReader reader = PooledNetworkReaders.Pull())
            {
                // Monta o cabeçalho dos dados e ler deus dados.
                reader.SetBuffer(StateObject.ReceivedDatagram);
                //**************************************************************************
                byte[] packetBuffer = reader.ReadSize(out int size); //* ler o pacote.
                if (size <= OthersHelper.GetConstants().MaxUdpPacketSize)
                {
                    int playerId = reader.ReadInt16(); //* ler o id do jogador que está transmitindo.
                    packetBuffer = packetBuffer.Decompress(); //* descomprime a porra do pacote.

                    RunPacket(Players[playerId], packetBuffer, out Packet packet); //* executa o pacote.
#if UNITY_EDITOR
                    //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                    NeutronStatistics.ClientUDP.AddIncoming(size, packet);
#endif
                }
                else
                    LogHelper.Error($"Packet size exceeds defined limit!! size: {size}");
            }
        }

        //* Ler os dados udp de modo assíncrono.
        private void UdpApmReceive()
        {
            if (TokenSource.Token.IsCancellationRequested)
                return;

            SocketHelper.BeginReadBytes(UdpClient, StateObject, UdpApmEndReceive);
        }

        private void UdpApmEndReceive(IAsyncResult ar)
        {
            EndPoint remoteEp = StateObject.NonAllocEndPoint;
            int bytesRead = SocketHelper.EndReadBytes(UdpClient, ref remoteEp, ar);
            if (bytesRead > 0)
            {
                StateObject.ReceivedDatagram = new byte[bytesRead];
                //********************************************************************************************
                Buffer.BlockCopy(StateObject.Buffer, 0, StateObject.ReceivedDatagram, 0, bytesRead);
                //********************************************************************************************
                CreateUdpPacket();
            }
            //Thread.Sleep(1);
            UdpApmReceive();
        }

        //* Metódo que ler os dados recebidos do servidor.
        private async void OnReceivingData(Stream networkStream, Protocol protocol)
        {
            try
            {
                CancellationToken token = TokenSource.Token;
                byte[] headerBuffer = new byte[NeutronModule.HeaderSize]; //* aqui será armazenado o pre-fixo(tamanho/length) do pacote, que é o tamanho da mensagem transmitida.
                byte[] playerIdBuffer = new byte[sizeof(short)]; //* aqui será armazenado o pre-fixo(ID) do jogador, que é o id do jogador que está transmitindo.
                if (protocol == Protocol.Udp)
                    StateObject = new StateObject(); //* Inicializa o objeto de estado do jogador.
                bool asynchronousApmMode = false;
                while (!token.IsCancellationRequested && !asynchronousApmMode) //* Interrompe o loop em caso de cancelamento do Token, o cancelamento ocorre em desconexões ou exceções ou em caso de Asynchronous APM Mode.
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                if (await SocketHelper.ReadAsyncBytes(networkStream, headerBuffer, 0, NeutronModule.HeaderSize, token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                                {
                                    int size = ByteHelper.ReadSize(headerBuffer); //* converte o buffer em inteiro.
                                    if (size <= OthersHelper.GetConstants().MaxTcpPacketSize)
                                    {
                                        byte[] packetBuffer = new byte[size]; //* cria um buffer com o tamanho da mensagem.
                                        if (await SocketHelper.ReadAsyncBytes(networkStream, packetBuffer, 0, size, token))  //* ler a mensagem/pacote e armazena no buffer de pacote.
                                        {
                                            packetBuffer = packetBuffer.Decompress(); //* Descomprimir os dados recebidos.
                                            if (await SocketHelper.ReadAsyncBytes(networkStream, playerIdBuffer, 0, sizeof(short), token)) //* ler o pre-fixo(ID) do jogador, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                                            {
                                                int playerId = BitConverter.ToInt16(playerIdBuffer, 0); //* converte o buffer em inteiro.
                                                if (playerId <= NeutronModule.Settings.GlobalSettings.MaxPlayers && playerId > 0)
                                                {
                                                    RunPacket(Players[playerId], packetBuffer, out Packet packet); //* executa o pacote.
#if UNITY_EDITOR
                                                    //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                                    NeutronStatistics.ClientTCP.AddIncoming(size, packet);
#endif
                                                }
                                                else
                                                    LogHelper.Error("Player not found!!!!");
                                            }
                                            else
                                                Dispose(); //* Fecha a conexão do cliente.
                                        }
                                        else
                                            Dispose(); //* Fecha a conexão do cliente.
                                    }
                                    else
                                        LogHelper.Error($"Packet size exceeds defined limit!! size: {size}");
                                }
                                else
                                    Dispose(); //* Fecha a conexão do cliente.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                //* precisa nem dizer nada aqui né?
                                switch (OthersHelper.GetConstants().ReceiveAsyncPattern)
                                {
                                    case AsynchronousType.TAP:
                                        if (await SocketHelper.ReadAsyncBytes(UdpClient, StateObject))
                                            CreateUdpPacket();
                                        break;
                                    default:
                                        UdpApmReceive();
                                        asynchronousApmMode = true;
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            //catch (SocketException) { }
            catch (Exception ex)
            {
                LogHelper.StackTrace(ex);
            }
        }

        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        private void RunPacket(NeutronPlayer player, byte[] buffer, out Packet outPacket)
        {
            using (NeutronReader reader = PooledNetworkReaders.Pull())
            {
                reader.SetBuffer(buffer);
                //*****************************************
                outPacket = (Packet)reader.ReadPacket();
                //*****************************************
                switch (outPacket)
                {
                    case Packet.Ping:
                        {
                            double serverTime = reader.ReadDouble();
                            double clientTime = reader.ReadDouble();
#if !UNITY_SERVER
                            double diff = Math.Abs(Time - clientTime);
                            if (!Application.isEditor || !NeutronServer.Initialized) //* O tempo só é cronemetado se não for o editor ou se o servidor no editor estiver desligado.
                            {
                                if (ClientMode == ClientMode.Virtual && Client != null)
                                { }
                                else
                                {
                                    double finalDiff = Math.Abs((serverTime - Time) + diff);
                                    if (serverTime > Time)
                                    {
                                        if (finalDiff > OthersHelper.GetConstants().TimeResyncTolerance)
                                            DiffTime += finalDiff;
                                    }
                                    else if (finalDiff > OthersHelper.GetConstants().TimeDesyncTolerance)
                                        DiffTime -= finalDiff;
                                }
                            }

                            if (_rtts.Count < 4)
                                _rtts.Add(diff * 1000);
                            else
                            {
                                RoundTripTime = _rtts.Sum() / _rtts.Count;
                                if (!(RoundTripTime < OthersHelper.GetConstants().MaxLatency))
                                    OnFail?.Invoke(Packet.Ping, "Your ping is too high!", this);
                                _rtts.Clear();
                            }
                            ++_pingAmountReceived;
#endif
                        }
                        break;
                    case Packet.Handshake:
                        {
                            double serverTime = reader.ReadDouble();
                            double clientTime = reader.ReadDouble();
#if !UNITY_SERVER
                            if (ClientMode == ClientMode.Virtual && Client != null)
                            { }
                            else
                            {
                                double diff = Math.Abs(Time - clientTime);
                                if (!Application.isEditor || !NeutronServer.Initialized) //* O tempo só é cronemetado se não for o editor ou se o servidor no editor estiver desligado.
                                {
                                    if (serverTime > Time)
                                        DiffTime = Math.Abs((serverTime - Time) + diff);
                                }
                                RoundTripTime = diff * 1000;
                            }
#endif
                            #region Reader
                            int port = reader.ReadInt32();
                            NeutronPlayer localPlayer = reader.ReadIntExactly<NeutronPlayer>();
                            NeutronPlayer[] othersPlayers = reader.ReadIntExactly<NeutronPlayer[]>();
                            #endregion

                            #region Udp
                            UdpEndPoint = new NonAllocEndPoint(IPAddress.Parse(_host), port);
                            //* Udp, envia um pacote 30 vezes(30 vezes por que pode ocorrer perda de pacotes, e queremos que pelo menos 1 chegue ao servidor.), para que seja feito o "Syn/Ack/Handshake"
                            using (NeutronWriter writer = PooledNetworkWriters.Pull())
                            {
                                for (int i = 0; i < 30; i++)
                                {
                                    writer.WritePacket((byte)Packet.Empty);
                                    Send(writer, Protocol.Udp);
                                }
                            }
                            #endregion

                            #region Logic
                            Player = localPlayer;
                            Player.IsConnected = true;
                            Players[Player.ID] = Player;
                            foreach (var currentPlayer in othersPlayers)
                            {
                                currentPlayer.IsConnected = true;
                                if (currentPlayer.Equals(Player))
                                    continue;
                                else
                                    Players[currentPlayer.ID] = currentPlayer;
                            }
                            OnPlayerConnected?.Invoke(Player, IsMine(Player), this);
                            _isInitialized = true;
                            #endregion
                        }
                        break;
                    case Packet.NewPlayer:
                        {
                            #region Reader
                            NeutronPlayer newPlayer = reader.ReadIntExactly<NeutronPlayer>();
                            #endregion

                            #region Logic
                            newPlayer.IsConnected = true;
                            Players[newPlayer.ID] = newPlayer;
                            #endregion

                            #region Event
                            OnPlayerConnected?.Invoke(newPlayer, IsMine(newPlayer), this);
                            #endregion
                        }
                        break;
                    case Packet.Disconnection:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            string reason = reader.ReadString();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            player.IsConnected = false;
                            #endregion

                            #region Event
                            OnPlayerDisconnected?.Invoke(reason, player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.Chat:
                        {
                            #region Reader
                            string message = reader.ReadString();
                            int playerId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            #endregion

                            #region Event
                            OnMessageReceived?.Invoke(message, player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.iRPC:
                        {
                            #region Reader
                            RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                            short viewID = reader.ReadInt16();
                            byte rpcId = reader.ReadByte();
                            byte instanceId = reader.ReadByte();
                            byte[] parameters = reader.ReadNextBytes(buffer.Length);
                            #endregion

                            #region Logic
                            iRPCHandler(rpcId, viewID, instanceId, parameters, player, registerType);
                            #endregion
                        }
                        break;
                    case Packet.gRPC:
                        {
                            #region Reader
                            byte rpcId = reader.ReadByte();
                            byte[] parameters = reader.ReadNextBytes(buffer.Length);
                            #endregion

                            #region Logic
                            gRPCHandler(rpcId, player, parameters, false, IsMine(player));
                            #endregion
                        }
                        break;
                    case Packet.GetChannels:
                        {
                            #region Reader
                            NeutronChannel[] channels = reader.ReadIntExactly<NeutronChannel[]>();
                            #endregion

                            #region Logic
                            OnChannelsReceived?.Invoke(channels, this);
                            #endregion
                        }
                        break;
                    case Packet.JoinChannel:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            NeutronChannel channel = reader.ReadIntExactly<NeutronChannel>();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnPlayerJoinedChannel?.Invoke(channel, player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.Leave:
                        {
                            #region Reader
                            MatchmakingMode packet = (MatchmakingMode)reader.ReadPacket();
                            int playerId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            if (packet == MatchmakingMode.Channel)
                                OnPlayerLeftChannel?.Invoke(reader.ReadIntExactly<NeutronChannel>(), player, IsMine(player), this);
                            else if (packet == MatchmakingMode.Room)
                                OnPlayerLeftRoom?.Invoke(reader.ReadIntExactly<NeutronRoom>(), player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.Fail:
                        {
                            #region Reader
                            Packet packet = (Packet)reader.ReadPacket();
                            string message = reader.ReadString();
                            #endregion

                            #region Logic
                            OnFail?.Invoke(packet, message, this);
                            #endregion
                        }
                        break;
                    case Packet.CreateRoom:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            NeutronRoom room = reader.ReadIntExactly<NeutronRoom>();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnPlayerCreatedRoom?.Invoke(room, player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.GetRooms:
                        {
                            #region Reader
                            NeutronRoom[] rooms = reader.ReadIntExactly<NeutronRoom[]>();
                            #endregion

                            #region Logic
                            OnRoomsReceived?.Invoke(rooms, this);
                            #endregion
                        }
                        break;
                    case Packet.JoinRoom:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            NeutronRoom room = reader.ReadIntExactly<NeutronRoom>();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnPlayerJoinedRoom?.Invoke(room, player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.DestroyPlayer:
                        {
                            #region Logic
                            #endregion
                        }
                        break;
                    case Packet.Nickname:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            string nickname = reader.ReadString();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnPlayerNicknameChanged?.Invoke(player, nickname, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.SetPlayerProperties:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnPlayerPropertiesChanged?.Invoke(player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.SetRoomProperties:
                        {
                            #region Reader
                            int playerId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            OnRoomPropertiesChanged?.Invoke(player, IsMine(player), this);
                            #endregion
                        }
                        break;
                    case Packet.CustomPacket:
                        {
                            #region Reader
                            CustomPacket packet = (CustomPacket)reader.ReadPacket();
                            int playerId = reader.ReadInt32();
                            byte[] parameters = reader.ReadIntExactly();
                            #endregion

                            #region Logic
                            //NeutronPlayer player = Players[playerId];
                            using (NeutronReader pReader = PooledNetworkReaders.Pull())
                            {
                                pReader.SetBuffer(parameters);
                                OnPlayerCustomPacketReceived?.Invoke(pReader, player, packet, this);
                            }
                            #endregion
                        }
                        break;
                    case Packet.OnAutoSync:
                        {
                            #region Reader
                            RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                            short viewID = reader.ReadInt16();
                            byte instanceId = reader.ReadByte();
                            byte[] parameters = reader.ReadNextBytes(buffer.Length);
                            #endregion

                            #region Logic
                            OnAutoSyncHandler(player, viewID, instanceId, parameters, registerType);
                            #endregion
                        }
                        break;
                }
            }
        }
        #endregion

        #region Methods -> Instance -> Packets
        //* Inicia uma pulsação para notificar que o cliente ainda está ativo.
        //* Se o servidor parar de receber esta pulsação o cliente será desconectado.
        private IEnumerator Ping()
        {
            yield return new WaitUntil(() => _isInitialized);
            while (!TokenSource.Token.IsCancellationRequested)
            {
                ++_pingAmount;
                using (NeutronWriter writer = PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.Ping);
                    writer.Write(Time);
                    Send(writer, Protocol.Tcp, Packet.Ping);
                }

                yield return _yieldPing;

                PacketLoss = _pingAmountReceived > 0
                    ? Math.Round((100 - (((double)_pingAmountReceived / (double)_pingAmount) * 100)) + (RoundTripTime / 1000))
                    : 0;
            }
        }

        //* Inicia uma pulsação para notificar que o cliente ainda está ativo.
        private IEnumerator TcpKeepAlive()
        {
            yield return new WaitUntil(() => _isInitialized);
            while (!TokenSource.Token.IsCancellationRequested)
            {
                using (NeutronWriter writer = PooledNetworkWriters.Pull())
                {
                    writer.WritePacket((byte)Packet.TcpKeepAlive);
                    Send(writer, Protocol.Tcp, Packet.TcpKeepAlive);
                }
                yield return _yieldTcpKeepAlive;
            }
        }

        /// <summary>
        ///* Sai da Sala, Canal ou Grupo.<br/>
        ///* A saída falhará se você não estiver em um canal, sala ou grupo.<br/>
        ///* Retorno de chamada: OnPlayerLeftChannel, OnPlayerLeftRoom, OnPlayerLeftGroup ou OnFail.<br/>
        /// </summary>
        /// <param name="packet">* O tipo do pacote de saída.</param>
        public void Leave(MatchmakingMode packet)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Leave);
                writer.WritePacket((byte)packet);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia uma mensagem de Chat para o túnel especificado.<br/>
        ///* O envio falhará se a mensagem for em branco, nulo.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/>
        /// </summary>
        /// <param name="message">* A mensagem que será enviada.</param>
        /// <param name="tunnelingTo">* O Túnel que será usado para a transmissão.</param>
        public void SendMessage(string message, TunnelingTo tunnelingTo)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Chat);
                writer.WritePacket((byte)ChatMode.Global);
                writer.WritePacket((byte)tunnelingTo);
                writer.Write(message);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia uma mensagem privada de chat para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir ou se a mensagem for nula, em branco.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/> 
        /// </summary>
        /// <param name="message">* A mensagem que será enviada.</param>
        /// <param name="player">* O jogador de destino da mensagem.</param>
        public void SendMessage(string message, NeutronPlayer player)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Chat);
                writer.WritePacket((byte)ChatMode.Private);
                writer.Write(player.ID);
                writer.Write(message);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para a rede.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="packet">* O Pacote personalizado que será usado.</param>
        /// <param name="targetTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="tunnelingTo">* O Túnel que será usado para a transmissão.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter parameters, CustomPacket packet, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.CustomPacket);
                writer.Write(Player.ID);
                writer.WritePacket((byte)packet);
                writer.WritePacket((byte)targetTo);
                writer.WritePacket((byte)tunnelingTo);
                writer.WritePacket((byte)recProtocol);
                writer.WriteIntWriter(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="player">* O jogador de destino do pacote.</param>
        /// <param name="packet">* O Pacote personalizado que será usado.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter parameters, NeutronPlayer player, CustomPacket packet, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.CustomPacket);
                writer.Write(player.ID);
                writer.WritePacket((byte)packet);
                writer.WritePacket((byte)recProtocol);
                writer.WriteIntWriter(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico utilizando seu objeto de rede.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="view">* O jogador de destino do pacote.</param>
        /// <param name="packet">* O Pacote personalizado que será usado.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter parameters, NeutronView view, CustomPacket packet, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.CustomPacket);
                writer.Write(view.Id);
                writer.WritePacket((byte)packet);
                writer.WritePacket((byte)recProtocol);
                writer.WriteIntWriter(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia o OnAutoSynchronization.<br/>
        /// </summary>
        /// <param name="packet">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="instanceId">* A Instância que invocará o metódo.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar o pacote.</param>
        /// <param name="isServerSide">* Attribuir este campo caso deseje enviar a partir do lado do servidor.</param>
        [Network(Size.AutoSync)]
        public void OnAutoSynchronization(INeutronWriter header, INeutronWriter packet, NeutronView view, byte instanceId, Protocol protocol, bool isServerSide = false)
        {
            if (packet.GetPosition() == 0)
            {
                packet.WritePacket((byte)Packet.OnAutoSync);
                packet.WritePacket((byte)view.RegisterType);
                packet.Write((short)view.Id);
                packet.Write(instanceId);
                if (!isServerSide)
                    Send(header, packet, protocol);
                else
                {
                    NeutronPacket serverPacket = PooledNetworkPackets.Pull();
                    serverPacket.Buffer = packet.ToArray();
                    serverPacket.Owner = view.Player;
                    serverPacket.Sender = view.Player;
                    serverPacket.Protocol = protocol;
                    serverPacket.Packet = Packet.Empty;
                    Server.OnSimulatingReceivingData(serverPacket);
                }
            }
            else
                LogHelper.Error("Invalid position, is not zero!");
        }

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="id">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
        /// <param name="player">* Attribuir este campo caso deseje enviar a partir do lado do servidor.</param>
#pragma warning disable IDE1006
        public void gRPC(byte id, NeutronWriter parameters, Protocol protocol, NeutronPlayer player = null)
#pragma warning restore IDE1006
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.gRPC);
                writer.Write(id);
                writer.WriteNextBytes(parameters);
                if (player == null)
                    Send(writer, protocol);
                else
                    Server.OnSimulatingReceivingData(new NeutronPacket(writer, player, player, protocol));
            }
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        /// </summary>
        /// <param name="rpcId">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="cache">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="targetTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006
        public void iRPC(NeutronWriter parameters, NeutronView view, byte rpcId, byte instanceId, CacheMode cache, TargetTo targetTo, Protocol protocol, bool isServerSide = false)
#pragma warning restore IDE1006
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.iRPC);
                writer.WritePacket((byte)view.RegisterType);
                writer.WritePacket((byte)targetTo);
                writer.WritePacket((byte)cache);
                writer.Write((short)view.Id);
                writer.Write(rpcId);
                writer.Write(instanceId);
                writer.WriteNextBytes(parameters);
                if (!isServerSide)
                    Send(writer, protocol);
                else
                    Server.OnSimulatingReceivingData(new NeutronPacket(writer, view.Player, view.Player, protocol));
            }
        }

        /// <summary>
        ///* Registra um nickname para seu jogador.<br/>
        ///* O registro falhará se o nickname é em branco ou nulo ou igual ao anterior.<br/>
        ///* Retorno de chamada: OnPlayerNicknameChanged ou OnFail.<br/>
        /// </summary>
        /// <param name="nickname">* O Nickname que você deseja registrar.</param>
        /// <param name="player">* Attribuir este valor caso deseje enviar a partir do lado do servidor.</param>
        public void SetNickname(string nickname, NeutronPlayer player = null)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.Nickname);
                writer.Write(nickname);
                if (player == null)
                    Send(writer);
                else
                    Server.OnSimulatingReceivingData(new NeutronPacket(writer, player, player, Protocol.Tcp));
            }
            Nickname = nickname;
        }

        /// <summary>
        ///* Ingressa em um canal pelo ID.<br/>
        ///* Se o ID for 0, ingressará em um canal aleatório.<br/>
        ///* A entrada em um canal falhará se o canal estiver cheio, fechado, não existente ou quando o usuário já estiver presente no canal.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        /// </summary>
        /// <param name="channelId">* O ID do canal que deseja ingressar.</param>
        public void JoinChannel(int channelId)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.JoinChannel);
                writer.Write(channelId);
                Send(writer);
            }
        }

        /// <summary>
        ///* Ingressa em uma sala pelo ID.<br/>
        ///* Se o ID for 0, ingressará em uma sala aleatória.<br/>
        ///* A entrada em uma sala falhará se a sala estiver cheia, fechada, não existente ou quando o usuário já estiver presente na sala.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        /// </summary>
        /// <param name="roomId">* O ID da sala que deseja ingressar.</param>
        public void JoinRoom(int roomId)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.JoinRoom);
                writer.Write(roomId);
                Send(writer);
            }
        }

        /// <summary>
        ///* Cria uma nova sala.<br/>
        ///* A criação da sala falhará se o nome for nulo, em branco ou se a quantidade máxima de salas foi antigida.<br/>
        ///* Retorno de chamada: OnPlayerCreatedRoom ou OnFail.<br/>
        /// </summary>
        public void CreateRoom(NeutronRoom room)
        {
            room.HasPassword = !string.IsNullOrEmpty(room.Password);
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.CreateRoom);
                writer.Write(room.Password);
                writer.WriteIntExactly(room);
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém os pacotes armazenados em cache.<br/>
        ///* Falhará se o ID especificado não for válido ou se não existir pacotes em cache.<br/>
        ///* Retorno de chamada: Nenhum.<br/>
        /// </summary>
        /// <param name="packet">* O tipo de pacote que deseja obter.</param>
        /// <param name="packetId">* ID do pacote que deseja obter os dados.</param>
        /// <param name="includeOwnerPackets">* Define se você deve receber pacotes em cache que são seus.</param>
        public void GetCachedPackets(CachedPacket packet, int packetId, bool includeOwnerPackets = true)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.GetChached);
                writer.WritePacket((byte)packet);
                writer.Write(packetId);
                writer.Write(includeOwnerPackets);
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém os canais disponíveis.<br/>
        ///* Falhará se não houver canais disponíveis.<br/>
        ///* Retorno de chamada: OnChannelsReceived ou OnFail.<br/>
        /// </summary>
        public void GetChannels()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.GetChannels);
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém as salas disponíveis.<br/>
        ///* Falhará se não houver salas disponíveis.<br/>
        ///* Retorno de chamada: OnRoomsReceived ou OnFail.<br/>
        /// </summary>
        public void GetRooms()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.GetRooms);
                Send(writer);
            }
        }

        /// <summary>
        ///* Define as propriedades do seu jogador.<br/>
        ///* Retorno de chamada: OnPlayerPropertiesChanged ou OnFail.<br/>
        /// </summary>
        /// <param name="properties">* O dicionário que contém as propriedades e valores a serem definidos para o jogador.</param>
        public void SetPlayerProperties(Dictionary<string, object> properties)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.SetPlayerProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer);
            }
        }

        /// <summary>
        ///* Define as propriedades da sala.<br/>
        ///* Retorno de chamada: OnRoomPropertiesChanged ou OnFail.<br/>
        /// </summary>
        /// <param name="properties">* O dicionário que contém as propriedades e valores a serem definidos para o jogador.</param>
        public void SetRoomProperties(Dictionary<string, object> properties)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.SetRoomProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer);
            }
        }

        /// <summary>
        ///* Destrói seu objeto de rede que representa seu jogador.<br/>
        /// </summary>
        public void DestroyPlayer()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket((byte)Packet.DestroyPlayer);
                Send(writer);
            }
        }
        #endregion

        #region Methods -> Static
        /// <summary>
        ///* Cria uma instância de Neutron que será usada para realizar a conexão e a comunicação com o servidor.<br/>
        ///* As instâncias são independentes, cada instância é uma conexão nova.<br/>
        /// </summary>
        /// <param name="clientMode">* O tipo de cliente que a instância usará.</param>
        /// <returns>Retorna uma instância do tipo Neutron.</returns>
        public static Neutron Create(ClientMode clientMode)
        {
            Neutron neutron = new Neutron();
            neutron.Initialize(clientMode);
            if (clientMode == ClientMode.Player)
            {
                if (Client == null)
                    Client = neutron;
                else
                    LogHelper.Error("The main player has already been initialized, you don't want to create a virtual client?");
            }
            return neutron;
        }

        /// <summary>
        ///* Spawna um objeto em rede.
        /// </summary>
        /// <param name="isServer">* Define se o lado a ser instaciado é o servidor ou o cliente.</param>
        /// <param name="prefab">* O Prefab que será usado para criar o objeto em rede.</param>
        /// <param name="position">* A posição que o objeto usará no momento de sua criação.</param>
        /// <param name="rotation">* A rotação que o objeto usará no momento de sua criação.</param>
        /// <returns>Retorna uma instância do tipo NeutronView.</returns>
        public static NeutronView Spawn(bool isServer, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            NeutronView Spawn() => MonoBehaviour.Instantiate(prefab, position, rotation).GetComponent<NeutronView>();
            if (prefab.TryGetComponent(out NeutronView neutronView))
            {
                switch (neutronView.Side)
                {
                    case Side.Both:
                        return Spawn();
                    case Side.Server:
                        return isServer ? Spawn() : null;
                    case Side.Client:
                        return !isServer ? Spawn() : null;
                    default:
                        return null;
                }
            }
            else
                return !LogHelper.Error("\"Neutron View\" object not found, failed to instantiate in network.") ? null : (NeutronView)null;
        }

        /// <summary>
        ///* Uma requisição POST é usado para enviar dados a um servidor para criar ou atualizar um recurso.
        /// </summary>
        /// <param name="url">O url no qual que será enviado a requisição.</param>
        /// <param name="formData">Os parâmetros que serão enviados para o metódo POST.</param>
        /// <param name="onResult">O resultado da requisição.</param>
        public static void Post(string url, WWWForm formData, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Post(url, formData);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }

        /// <summary>
        ///* A requisição GET é usado para solicitar dados de um recurso especificado. 
        /// </summary>
        /// <param name="url">O url no qual que será enviado a requisição.</param>
        /// <param name="onResult">O resultado da requisição.</param>
        public static void Get(string url, Action<UnityWebRequest> onResult)
        {
            IEnumerator Request()
            {
                UnityWebRequest request = UnityWebRequest.Get(url);
                yield return request.SendWebRequest();
                onResult.Invoke(request);
            }
            NeutronSchedule.ScheduleTask(Request());
        }
        #endregion
    }
}