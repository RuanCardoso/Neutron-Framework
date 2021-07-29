using NeutronNetwork.Client;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Json;
using NeutronNetwork.Server;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

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
        #region Fields -> Collections
        /// <summary>
        ///* Providencia um pool de escritores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronWriter> PooledNetworkWriters = new NeutronPool<NeutronWriter>(() => new NeutronWriter());
        /// <summary>
        ///* Providencia um pool de leitores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronReader> PooledNetworkReaders = new NeutronPool<NeutronReader>(() => new NeutronReader());
        //* Usado para retirar a media do RTT;
        private readonly List<double> _rtts = new List<double>();
        #endregion

        #region Fields -> Primitives
        //* Quantidade de pacotes perdidos.
        private int _pingAmountReceived;
        //* Quantidade de tentativas de ping.
        private int _pingAmount;
        //* Address de conexão.
        private string _host;
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
        ///* Retorna a quantidade de pacotes que falharam.
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

        #region Methods -> Instance
        /// <summary>
        ///* Inicia uma tentativa de estabelecer uma conexão com o servidor.
        /// </summary>
        /// <param name="index">* Index do endereço de ip da lista de configurações</param>
        /// <param name="timeout">* Tempo limite de tentativa de estabelecer uma conexão.</param>
        /// <returns></returns>
        public async void Connect(int index = 0, int timeout = 3)
        {
            await Task.Delay(100); //* Um delay para nada ocorrer errado ao inicializar Servidor e Cliente no Editor.
#if UNITY_SERVER
            if (m_ClientType == ClientType.Player)
                NeutronLogger.LoggerError($"MainClient disabled in server!\r\n");
            return;
#endif
            Initialize(); //* Inicializa o cliente.
            TcpClient.NoDelay = NeutronMain.Settings.GlobalSettings.NoDelay; //* Define um valor que desabilita o atraso ao enviar ou receber buffers que não estão cheios.

            //* Simulação de perda de pacotes.
            if (NeutronMain.Settings.LagSimulationSettings.Drop)
                OthersHelper.Odds(NeutronMain.Settings.LagSimulationSettings.Percent);

            //* Obtém o ip do URL setado nas configurações.
            #region Host Resolver
            _host = NeutronMain.Settings.GlobalSettings.Addresses[index];
            int port = NeutronMain.Settings.GlobalSettings.Port;
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
                    else _host = IPAddress.Loopback.ToString();
                }
                else { /*Continue*/ }
            }
            else _host = IPAddress.Loopback.ToString();
            #endregion

            if (!IsConnected)
            {
                bool result = await TcpClient.ConnectAsync(_host, port).RunWithTimeout(new TimeSpan(0, 0, timeout));

                if (!result)
                {
                    if (!LogHelper.Error("An attempt to establish a connection to the server failed."))
                        Dispose();
                }
                else if (result)
                {
                    IsConnected = TcpClient.Connected;
                    //* Envia um pacote de reconhecimento pro servidor e ele responde com suas informações.
                    #region Handshake
                    using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                    {
                        writer.WritePacket(Packet.Handshake);
                        writer.Write(Time);
                        Send(writer);
                        writer.SetLength(0); //* Limpa o escritor.
                        writer.WritePacket(Packet.Empty);
                        for (int i = 0; i < 10; i++)
                            Send(writer, Protocol.Udp);
                    }
                    #endregion

                    //* Dedica um thread a receber os dados do servidor.
                    #region Threading
                    Thread clientTh = new Thread(() =>
                    {
                        OnReceivingData(Protocol.Tcp); //* Inicia a leitura dos dados Tcp.
                        OnReceivingData(Protocol.Udp); //* Inicia a leitura dos dados Ucp.
                    })
                    {
                        Priority = System.Threading.ThreadPriority.Highest,
                        IsBackground = true,
                        Name = "ClientTh",
                    };
                    clientTh.Start();
                    #endregion

                    NeutronSchedule.Schedule.StartCoroutine(Ping(Protocol.Udp, NeutronMain.Settings.ClientSettings.PingRate));
                }
                OnNeutronConnected?.Invoke(IsConnected, this);
            }
            else
                LogHelper.Error("Connection Refused!");
        }

        //* Metódo que ler os dados recebidos do servidor.
        private async void OnReceivingData(Protocol protocol)
        {
            try
            {
                CancellationToken token = _cts.Token;
                NetworkStream netStream = TcpClient.GetStream();

                byte[] hBuffer = new byte[sizeof(int)]; //* aqui será armazenado o pre-fixo do cabeçalho, que é o tamanho da mensagem enviada pelo servidor.

                while (!token.IsCancellationRequested)
                {
                    if (NeutronMain.Settings.LagSimulationSettings.Inbound)
                        await Task.Delay(NeutronMain.Settings.LagSimulationSettings.InOutDelay);

                    if (protocol == Protocol.Tcp)
                    {
                        if (await SocketHelper.ReadAsyncBytes(netStream, hBuffer, 0, sizeof(int), token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                        {
                            int size = BitConverter.ToInt32(hBuffer, 0); //* converte o buffer do pre-fixo de volta em inteiro.
                            if (size > 0)
                            {
                                byte[] mBuffer = new byte[size]; //* cria um buffer com o tamanho da mensagem/pre-fixo.
                                if (await SocketHelper.ReadAsyncBytes(netStream, mBuffer, 0, size, token))  //* ler a mensagem e armazena no buffer de mensagem.
                                {
                                    RunPacket(mBuffer); //* Processa os dados recebidos.
                                    {
                                        //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                        NeutronStatistics.m_ClientTCP.AddIncoming(size);
                                    }
                                }
                                else Dispose(); //* Fecha a conexão do cliente.
                            }
                            else Dispose(); //* Fecha a conexão do cliente.
                        }
                        else Dispose(); //* Fecha a conexão do cliente.
                    }
                    else if (protocol == Protocol.Udp)
                    {
                        var datagram = await UdpClient.ReceiveAsync();  //* Recebe os dados enviados pelo servidor.
                        if (datagram.Buffer.Length > 0)
                        {
                            RunPacket(datagram.Buffer); //* Processa os dados recebidos.
                            {
                                //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                NeutronStatistics.m_ClientUDP.AddIncoming(datagram.Buffer.Length);
                            }
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (Exception ex)
            {
                if (!LogHelper.Error("OnReceiveData exception!"))
                    LogHelper.StackTrace(ex);
            }
        }

        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        private void RunPacket(byte[] buffer)
        {
            try
            {
                using (NeutronReader reader = PooledNetworkReaders.Pull())
                {
                    reader.SetBuffer(buffer);
                    switch (reader.ReadPacket<Packet>())
                    {
                        case Packet.Ping:
                            {
                                double serverTime = reader.ReadDouble();
                                double clientTime = reader.ReadDouble();
#if !UNITY_SERVER
                                double diff = Math.Abs(Time - clientTime);
                                if (!Application.isEditor || !NeutronServer.Initialized)
                                {
                                    if (ClientType == global::Client.Virtual && Client != null)
                                    { }
                                    else
                                    {
                                        double finalDiff = Math.Abs((serverTime - Time) + diff);
                                        if (serverTime > Time)
                                        {
                                            if (finalDiff > OthersHelper.GetSettings().RESYNC_TOLERANCE)
                                                DiffTime += finalDiff;
                                        }
                                        else if (finalDiff > OthersHelper.GetSettings().NET_TIME_DESYNC_TOLERANCE)
                                            DiffTime -= finalDiff;
                                    }
                                }

                                if (_rtts.Count < 4)
                                    _rtts.Add(diff * 1000);
                                else
                                {
                                    RoundTripTime = _rtts.Sum() / _rtts.Count;
                                    if (!(RoundTripTime < OthersHelper.GetSettings().MAX_LATENCY))
                                        OnFail?.Invoke(Packet.Ping, "Your ping is too high.", this);
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
                                if (ClientType == global::Client.Virtual && Client != null)
                                { }
                                else
                                {
                                    double diff = Math.Abs(Time - clientTime);
                                    if (!Application.isEditor || !NeutronServer.Initialized)
                                    {
                                        if (serverTime > Time)
                                            DiffTime = Math.Abs((serverTime - Time) + diff);
                                    }
                                    RoundTripTime = diff * 1000;
                                }
#endif
                                #region Reader
                                int port = reader.ReadInt32();
                                NeutronPlayer player = reader.ReadExactly<NeutronPlayer>();
                                NeutronPlayer[] players = reader.ReadExactly<NeutronPlayer[]>();
                                #endregion

                                #region Udp
                                _udpEndPoint = new IPEndPoint(IPAddress.Parse(_host), port);
                                #endregion

                                #region Logic
                                Player = player;
                                Player.IsConnected = true;
                                Players[Player.ID] = Player;
                                foreach (var currentPlayer in players)
                                {
                                    currentPlayer.IsConnected = true;
                                    if (currentPlayer.Equals(Player))
                                        continue;
                                    else
                                        Players[currentPlayer.ID] = currentPlayer;
                                }
                                OnPlayerConnected?.Invoke(Player, IsMine(Player), this);
                                #endregion
                            }
                            break;
                        case Packet.NewPlayer:
                            {
                                #region Reader
                                NeutronPlayer player = reader.ReadExactly<NeutronPlayer>();
                                #endregion

                                #region Logic
                                player.IsConnected = true;
                                Players[player.ID] = player;
                                #endregion

                                #region Event
                                OnPlayerConnected?.Invoke(player, IsMine(player), this);
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
                                NeutronPlayer player = Players[playerId];
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
                                NeutronPlayer player = Players[playerId];
                                #endregion

                                #region Event
                                OnMessageReceived?.Invoke(message, player, IsMine(player), this);
                                #endregion
                            }
                            break;
                        case Packet.iRPC:
                            {
                                #region Reader
                                int viewID = reader.ReadInt32();
                                int rpcId = reader.ReadInt32();
                                int playerId = reader.ReadInt32();
                                byte[] parameters = reader.ReadExactly();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                iRPCHandler(rpcId, viewID, parameters, true, player);
                                #endregion
                            }
                            break;
                        case Packet.gRPC:
                            {
                                #region Reader
                                int rpcId = reader.ReadInt32();
                                int playerId = reader.ReadInt32();
                                byte[] parameters = reader.ReadExactly();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                gRPCHandler(rpcId, player, parameters, false, IsMine(player));
                                #endregion
                            }
                            break;
                        case Packet.GetChannels:
                            {
                                #region Reader
                                NeutronChannel[] channels = reader.ReadExactly<NeutronChannel[]>();
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
                                NeutronChannel channel = reader.ReadExactly<NeutronChannel>();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                OnPlayerJoinedChannel?.Invoke(channel, player, IsMine(player), this);
                                #endregion
                            }
                            break;
                        case Packet.Leave:
                            {
                                #region Reader
                                MatchmakingPacket packet = reader.ReadPacket<MatchmakingPacket>();
                                int playerId = reader.ReadInt32();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                if (packet == MatchmakingPacket.Channel)
                                    OnPlayerLeftChannel?.Invoke(reader.ReadExactly<NeutronChannel>(), player, IsMine(player), this);
                                else if (packet == MatchmakingPacket.Room)
                                    OnPlayerLeftRoom?.Invoke(reader.ReadExactly<NeutronRoom>(), player, IsMine(player), this);
                                #endregion
                            }
                            break;
                        case Packet.Fail:
                            {
                                #region Reader
                                Packet systemPacket = reader.ReadPacket<Packet>();
                                string message = reader.ReadString();
                                #endregion

                                #region Logic
                                LogHelper.Error($"[{systemPacket}] -> | ERROR | {message}");
                                #endregion

                                #region Event
                                OnFail?.Invoke(systemPacket, message, this);
                                #endregion
                            }
                            break;
                        case Packet.CreateRoom:
                            {
                                #region Reader
                                int playerId = reader.ReadInt32();
                                NeutronRoom room = reader.ReadExactly<NeutronRoom>();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                OnPlayerCreatedRoom?.Invoke(room, player, IsMine(player), this);
                                #endregion
                            }
                            break;
                        case Packet.GetRooms:
                            {
                                #region Reader
                                NeutronRoom[] rooms = reader.ReadExactly<NeutronRoom[]>();
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
                                NeutronRoom room = reader.ReadExactly<NeutronRoom>();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
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
                                NeutronPlayer player = Players[playerId];
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
                                NeutronPlayer player = Players[playerId];
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
                                NeutronPlayer player = Players[playerId];
                                OnRoomPropertiesChanged?.Invoke(player, IsMine(player), this);
                                #endregion
                            }
                            break;
                        case Packet.CustomPacket:
                            {
                                #region Reader
                                CustomPacket packet = reader.ReadPacket<CustomPacket>();
                                int playerId = reader.ReadInt32();
                                byte[] parameters = reader.ReadExactly();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
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
                                RegisterType registerType = reader.ReadPacket<RegisterType>();
                                int playerId = reader.ReadInt32();
                                int viewID = reader.ReadInt32();
                                int instanceId = reader.ReadInt32();
                                byte[] parameters = reader.ReadExactly();
                                #endregion

                                #region Logic
                                NeutronPlayer player = Players[playerId];
                                OnAutoSyncHandler(player, viewID, instanceId, parameters, registerType);
                                #endregion
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }
        #endregion

        #region Methods -> Instance -> Packets
        //* Inicia uma pulsação para notificar que o cliente ainda está ativo.
        //* Se o servidor parar de receber esta pulsação o cliente será desconectado.
        private IEnumerator Ping(Protocol protocol, float delay)
        {
            yield return new WaitUntil(() => (Player != null && Player.IsConnected));
            while (!_cts.Token.IsCancellationRequested)
            {
                ++_pingAmount;
                using (NeutronWriter writer = PooledNetworkWriters.Pull())
                {
                    writer.WritePacket(Packet.Ping);
                    writer.Write(Time);
                    Send(writer, protocol);
                }

                yield return new WaitForSeconds(delay);

                if (_pingAmountReceived > 0)
                    PacketLoss = Math.Round((100 - (((double)_pingAmountReceived / (double)_pingAmount) * 100)) + (RoundTripTime / 1000));
                else
                    PacketLoss = NeutronMain.Settings.LagSimulationSettings.Percent;
            }
        }

        /// <summary>
        ///* Sai da Sala, Canal ou Grupo.<br/>
        ///* A saída falhará se você não estiver em um canal, sala ou grupo.<br/>
        ///* Retorno de chamada: OnPlayerLeftChannel, OnPlayerLeftRoom, OnPlayerLeftGroup ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="packet">* O tipo do pacote de saída.</param>
        public void Leave(MatchmakingPacket packet)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Leave);
                writer.WritePacket(packet);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia uma mensagem de Chat para o túnel especificado.<br/>
        ///* O envio falhará se a mensagem for em branco, nulo.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="message">* A mensagem que será enviada.</param>
        /// <param name="tunnelingTo">* O Túnel que será usado para a transmissão.</param>
        public void SendMessage(string message, TunnelingTo tunnelingTo)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Chat);
                writer.WritePacket(ChatPacket.Global);
                writer.WritePacket(tunnelingTo);
                writer.Write(message);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia uma mensagem privada de chat para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir ou se a mensagem for nula, em branco.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/> 
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="message">* A mensagem que será enviada.</param>
        /// <param name="player">* O jogador de destino da mensagem.</param>
        public void SendMessage(string message, NeutronPlayer player)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Chat);
                writer.WritePacket(ChatPacket.Private);
                writer.Write(player.ID);
                writer.Write(message);
                Send(writer);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para a rede.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
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
                writer.WritePacket(Packet.CustomPacket);
                writer.Write(Player.ID);
                writer.WritePacket(packet);
                writer.WritePacket(targetTo);
                writer.WritePacket(tunnelingTo);
                writer.WritePacket(recProtocol);
                writer.Write(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
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
                writer.WritePacket(Packet.CustomPacket);
                writer.Write(player.ID);
                writer.WritePacket(packet);
                writer.WritePacket(recProtocol);
                writer.Write(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico utilizando seu objeto de rede.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
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
                writer.WritePacket(Packet.CustomPacket);
                writer.Write(view.ID);
                writer.WritePacket(packet);
                writer.WritePacket(recProtocol);
                writer.Write(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Envia o OnSerializeNeutronView.<br/>
        /// </summary>
        /// <param name="parameters">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="id">* A Instância que invocará o metódo.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void OnAutoSynchronization(NeutronWriter parameters, NeutronView view, int id, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.OnAutoSync);
                writer.WritePacket(recProtocol);
                writer.WritePacket(view.RegisterType);
                writer.WritePacket(targetTo);
                writer.WritePacket(tunnelingTo);
                writer.Write(view.ID);
                writer.Write(id);
                writer.Write(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* gRPC(Global Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="viewId">* ID do objeto de rede que será usado para transmitir os dados.</param>
        /// <param name="rpcId">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public void gRPC(int viewId, int rpcId, NeutronWriter parameters, Protocol protocol)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.gRPC);
                writer.Write(viewId);
                writer.Write(rpcId);
                writer.Write(parameters);
                Send(writer, protocol);
            }
        }

        /// <summary>
        ///* iRPC(Instance Remote Procedure Call), usado para a comunicação, isto é, a troca de dados ou sincronização via rede.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="viewId">* ID do objeto de rede que será usado para identificar a instância que deve invocar o metódo.</param>
        /// <param name="rpcId">* ID do metódo que será invocado.</param>
        /// <param name="parameters">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="cache">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="targetTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="tunnelingTo">* O Túnel que será usado para a transmissão.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar os dados.</param>
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public void iRPC(int viewId, int rpcId, NeutronWriter parameters, Cache cache, TargetTo targetTo, TunnelingTo tunnelingTo, Protocol recProtocol, Protocol sendProtocol)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.iRPC);
                writer.WritePacket(tunnelingTo);
                writer.WritePacket(targetTo);
                writer.WritePacket(cache);
                writer.WritePacket(recProtocol);
                writer.Write(viewId);
                writer.Write(rpcId);
                writer.Write(parameters);
                Send(writer, sendProtocol);
            }
        }

        /// <summary>
        ///* Registra um nickname para seu jogador.<br/>
        ///* O registro falhará se o nickname é em branco ou nulo ou igual ao anterior.<br/>
        ///* Retorno de chamada: OnPlayerNicknameChanged ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="nickname">* O Nickname que você deseja registrar.</param>
        public void SetNickname(string nickname)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Nickname);
                writer.Write(nickname);
                Send(writer);
            }
            Nickname = nickname;
        }

        /// <summary>
        ///* Ingressa em um canal pelo ID.<br/>
        ///* Se o ID for 0, ingressará em um canal aleatório.<br/>
        ///* A entrada em um canal falhará se o canal estiver cheio, fechado, não existente ou quando o usuário já estiver presente no canal.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="channelId">* O ID do canal que deseja ingressar.</param>
        public void JoinChannel(int channelId)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.JoinChannel);
                writer.Write(channelId);
                Send(writer);
            }
        }

        /// <summary>
        ///* Ingressa em uma sala pelo ID.<br/>
        ///* Se o ID for 0, ingressará em uma sala aleatória.<br/>
        ///* A entrada em uma sala falhará se a sala estiver cheia, fechada, não existente ou quando o usuário já estiver presente na sala.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="roomId">* O ID da sala que deseja ingressar.</param>
        public void JoinRoom(int roomId)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.JoinRoom);
                writer.Write(roomId);
                Send(writer);
            }
        }

        /// <summary>
        ///* Cria uma nova sala.<br/>
        ///* A criação da sala falhará se o nome for nulo, em branco ou se a quantidade máxima de jogadores foi antigida.<br/>
        ///* Retorno de chamada: OnPlayerCreatedRoom ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="roomName">* O Nome que será exibido para os jogadores do lobby.</param>
        /// <param name="maxPlayers">* A quantidade máxima de jogadores permitida.</param>
        /// <param name="password">* A senha que os jogadores usarão para ingressar na sala.</param>
        /// <param name="properties">* As propriedades da sala, ex: Tempo, Kills, Deaths.</param>
        /// <param name="isVisible">* Define se a sala é visivel em lobby.</param>
        /// <param name="joinOrCreate">* Define se deve criar uma sala nova ou ingressar se uma sala com o mesmo nome já existe.</param>
        public void CreateRoom(string roomName, int maxPlayers, string password, Dictionary<string, object> properties, bool isVisible = true, bool joinOrCreate = false)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.CreateRoom);
                writer.Write(roomName);
                writer.Write(maxPlayers);
                writer.Write(password ?? string.Empty);
                writer.Write(isVisible);
                writer.Write(joinOrCreate);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém os pacotes armazenados em cache.<br/>
        ///* Falhará se o ID especificado não for válido ou se não existir pacotes em cache.<br/>
        ///* Retorno de chamada: Nenhum.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        /// <param name="packet">* O tipo de pacote que deseja obter.</param>
        /// <param name="packetId">* ID do pacote que deseja obter os dados.</param>
        /// <param name="includeOwnerPackets">* Define se você deve receber pacotes em cache que são seus.</param>
        public void GetCachedPackets(CachedPacket packet, int packetId, bool includeOwnerPackets = true)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.GetChached);
                writer.WritePacket(packet);
                writer.Write(packetId);
                writer.Write(includeOwnerPackets);
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém os canais disponíveis.<br/>
        ///* Falhará se não houver canais disponíveis.<br/>
        ///* Retorno de chamada: OnChannelsReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        public void GetChannels()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.GetChannels);
                Send(writer);
            }
        }

        /// <summary>
        ///* Obtém as salas disponíveis.<br/>
        ///* Falhará se não houver salas disponíveis.<br/>
        ///* Retorno de chamada: OnRoomsReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        public void GetRooms()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.GetRooms);
                Send(writer);
            }
        }

        /// <summary>
        ///* Define as propriedades do seu jogador.<br/>
        ///* Retorno de chamada: OnPlayerPropertiesChanged ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        /// <param name="properties">* O dicionário que contém as propriedades e valores a serem definidos para o jogador.</param>
        public void SetPlayerProperties(Dictionary<string, object> properties)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.SetPlayerProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer);
            }
        }

        /// <summary>
        ///* Define as propriedades da sala.<br/>
        ///* Retorno de chamada: OnRoomPropertiesChanged ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        /// <param name="properties">* O dicionário que contém as propriedades e valores a serem definidos para o jogador.</param>
        public void SetRoomProperties(Dictionary<string, object> properties)
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.SetRoomProperties);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer);
            }
        }

        /// <summary>
        ///* Destrói seu objeto de rede que representa seu jogador.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        public void DestroyPlayer()
        {
            using (NeutronWriter writer = PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.DestroyPlayer);
                Send(writer);
            }
        }
        #endregion

        #region Methods -> Static
        /// <summary>
        ///* Cria uma instância de Neutron que será usada para realizar a conexão e a comunicação com o servidor.<br/>
        ///* As instâncias são independentes, cada instância é uma conexão nova.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="clientType">* O tipo de cliente que a instância usará.</param>
        /// <returns>Retorna uma instância do tipo Neutron.</returns>
        public static Neutron CreateClient(global::Client clientType)
        {
            Neutron instance = new Neutron();
            instance.Initialize(clientType);
            if (clientType == global::Client.Player)
                Client = instance;
            return instance;
        }

        /// <summary>
        ///* Cria um objeto em rede.
        /// </summary>
        /// <param name="isServer">* Define se o lado a ser instaciado é o servidor ou o cliente.</param>
        /// <param name="prefab">* O Prefab que será usado para criar o objeto em rede.</param>
        /// <param name="position">* A posição que o objeto usará no momento de sua criação.</param>
        /// <param name="rotation">* A rotação que o objeto usará no momento de sua criação.</param>
        /// <returns>Retorna uma instância do tipo NeutronView.</returns>
        public static NeutronView Spawn(bool isServer, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            NeutronView Spawn() => MonoBehaviour.Instantiate(prefab, position, rotation).GetComponent<NeutronView>();
            #region Logic
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
            #endregion
        }
        #endregion
    }
}