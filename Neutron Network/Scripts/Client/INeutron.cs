using System;
using System.Collections.Generic;
using System.Net;
using System.Net.NetworkInformation;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using Newtonsoft.Json;
using System.Threading.Tasks;
using System.Collections;
using NeutronNetwork.Client;
using NeutronNetwork.Constants;
using NeutronNetwork.Server;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Helpers;

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
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CLIENT)]
    public class Neutron : NeutronClientPublicFunctions
    {
        #region Pool
        /// <summary>
        ///* Providencia um pool de escritores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronWriter> PooledNetworkWriters = new NeutronPool<NeutronWriter>(() => new NeutronWriter());
        /// <summary>
        ///* Providencia um pool de leitores, utilize-o para melhor performance.
        /// </summary>
        public static NeutronPool<NeutronReader> PooledNetworkReaders = new NeutronPool<NeutronReader>(() => new NeutronReader());
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna a instância do servidor.<br/>
        ///* Esta propriedade só pode ser usada dentro dos pré-processadores #if UNITY_EDITOR || UNITY_SERVER<br/>
        ///! Só pode ser obtido no lado do servidor ou no Unity Editor, no cliente será nulo.
        /// </summary>
        public static NeutronServer Server
        {
            get
            {
#if !UNITY_EDITOR && !UNITY_SERVER
                NeutronLogger.Print("You cannot access the server's methods and properties on the client, except within the Unity Editor.");
#endif
                return NeutronServerFunctions._;
            }
        }

        /// <summary>
        ///* Retorna a instância principal do Cliente.
        /// </summary>
        public static Neutron Client { get; private set; }

        #region Classes
        /// <summary>
        ///* Retorna seu identificador de rede, disponível apenas quando o jogador é instaciado.
        /// </summary>
        public NeutronView NeutronView { get; set; }
        /// <summary>
        ///* Retorna o objeto que representa seu jogador.
        /// </summary>
        public Player MyPlayer => _myPlayer;
        /// <summary>
        ///* Retorna a sala em que você está ingressado.
        /// </summary>
        public Room CurrentRoom { get; set; }
        /// <summary>
        ///* Retorna o canal em que você está ingressado.
        /// </summary>
        public Channel CurrentChannel { get; set; }
        #endregion

        #region Fields
        private string m_Nickname;
        #endregion

        /// <summary>
        ///* Retorna o status da sua conexão.
        /// </summary>
        public bool IsConnected { get; private set; }
        /// <summary>
        ///* Obtém o nickname do seu jogador.
        /// </summary>
        public string Nickname => m_Nickname;
        #endregion

        #region Events
        /// <summary>
        ///* Este evento é acionado quando uma tentativa de conexão retorna seu estado.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<bool, Neutron> OnNeutronConnected = new NeutronEventNoReturn<bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador se conecta ao servidor.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerConnected = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador se desconecta do servidor.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<string, Player, bool, Neutron> OnPlayerDisconnected = new NeutronEventNoReturn<string, Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador envia uma mensagem e esta mensagem é recebida.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<string, Player, bool, Neutron> OnMessageReceived = new NeutronEventNoReturn<string, Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando a lista de canais disponíveis é recebida ou atualizada.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Channel[], Neutron> OnChannelsReceived = new NeutronEventNoReturn<Channel[], Neutron>();
        /// <summary>
        ///* Este evento é acionado quando a lista de salas disponíveis é recebida ou atualizada.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Room[], Neutron> OnRoomsReceived = new NeutronEventNoReturn<Room[], Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador sai do canal.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerLeftChannel = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador sai da sala.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerLeftRoom = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador cria uma sala.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Room, Neutron> OnPlayerCreatedRoom = new NeutronEventNoReturn<Room, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador entra na sala.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerJoinedRoom = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador entra no canal.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerJoinedChannel = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador é instanciado.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, GameObject, bool, Neutron> OnPlayerInstantiated = new NeutronEventNoReturn<Player, GameObject, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador é destruído.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Neutron> OnPlayerDestroyed = new NeutronEventNoReturn<Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador troca seu nickname.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerNicknameChanged = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador atualiza suas propriedades.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnPlayerPropertiesChanged = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando alguma sala atualiza suas propriedades.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<Player, bool, Neutron> OnRoomPropertiesChanged = new NeutronEventNoReturn<Player, bool, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum jogador envia um pacote personalizado.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<NeutronReader, Player, ClientPacket, Neutron> OnPlayerPacketReceived = new NeutronEventNoReturn<NeutronReader, Player, ClientPacket, Neutron>();
        /// <summary>
        ///* Este evento é acionado quando algum pacote apresenta uma falha.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        public static NeutronEventNoReturn<SystemPacket, string, Neutron> OnFail = new NeutronEventNoReturn<SystemPacket, string, Neutron>();
        #endregion

        #region Stats
        //* Quantidade de pacotes perdidos.
        private int m_PacketLoss;
        //* Quantidade de tentativas de ping.
        private int m_PingAmount;
        //* Seu ping atual com o servidor.
        private long m_Ping;
        #endregion

        #region Timer
        /// <summary>
        ///* Tempo atual do cliente/servidor, o tempo entre servidor e o cliente é sincronizado.
        /// </summary>
        public double CurrentTime { get; set; }
        //* Ultima atualização de tempo do servidor.
        private double nLastTime { get; set; }
        //* Diferença de tempo entre o cliente e o servidor.
        private double nDiffTime { get; set; }
        #endregion

        #region MonoBehaviour
        private void Update() => CurrentTime = Math.Abs(((double)Time.unscaledTime + nDiffTime));
        #endregion

        /// <summary>
        ///* Inicia uma tentativa de estabelecer uma conexão com o servidor.
        /// </summary>
        /// <param name="nHost">* Ip do servidor, opcional, por padrão usa o ip das configurações.</param>
        /// <param name="nPort">* Porta do servidor, opcional, por padrão usa a porta das configurações.</param>
        /// <param name="nTimeout">* Tempo limite de tentativa de conexão.</param>
        /// <returns></returns>
        public async void Connect(string nHost = "Settings", int nPort = 0, int nTimeout = 3)
        {
#if UNITY_SERVER
            NeutronLogger.LoggerError($"MainClient disabled in server!\r\n");
            return;
#endif

            InitializeSocket(); //* Inicializa o cliente.
            TcpSocket.NoDelay = NeutronConfig.Settings.GlobalSettings.NoDelay;

            //* Obtém o ip do URL setado nas configurações.
            #region Host Resolver
            string Host = NeutronConfig.Settings.GlobalSettings.Address;
            int Port = NeutronConfig.Settings.GlobalSettings.Port;
            if (nHost == "Settings" && nPort == 0)
            {
                if (!string.IsNullOrEmpty(Host))
                {
                    if (!IPAddress.TryParse(Host, out IPAddress _))
                    {
                        if (!Host.Equals("localhost", StringComparison.InvariantCultureIgnoreCase))
                        {
                            Host = Host.Replace("http://", string.Empty);
                            Host = Host.Replace("https://", string.Empty);
                            Host = Host.Replace("/", string.Empty);
                            Host = (await SocketHelper.GetHostAddress(Host)).ToString();
                        }
                        else Host = IPAddress.Loopback.ToString();
                    }
                }
                else Host = IPAddress.Loopback.ToString();
            }
            else
            {
                Host = nHost;
                Port = nPort;
            }

            NeutronConfig.Settings.GlobalSettings.Address = Host;
            NeutronConfig.Settings.GlobalSettings.Port = Port;
            #endregion

            if (!IsConnected)
            {
                var ConnectTask = TcpSocket.ConnectAsync(Host, Port);
                var DelayTask = Task.Delay(new TimeSpan(0, 0, nTimeout));

                await await Task.WhenAny(ConnectTask, DelayTask);

                if (DelayTask.IsCompleted)
                {
                    #region Dispose
                    Dispose();
                    #endregion
                    IsConnected = false;
                    NeutronLogger.Print("An attempt to establish a connection to the server failed.");
                }
                else if (ConnectTask.IsCompleted)
                {
                    IsConnected = true;
                    Handshake();

                    //* Dedica um thread a receber os dados do servidor.
                    #region Threading
                    Thread clientTh = new Thread(() =>
                    {
                        OnReceiveData(Protocol.Tcp, _cts.Token); //* Inicia a leitura dos dados Tcp.
                        OnReceiveData(Protocol.Udp, _cts.Token); //* Inicia a leitura dos dados Ucp.
                    })
                    {
                        Priority = System.Threading.ThreadPriority.Highest,
                        IsBackground = true,
                        Name = "ClientTh",
                    };
                    clientTh.Start();
                    #endregion

                    StartCoroutine(Heartbeat(Protocol.Udp, 1f));
                    StartCoroutine(Heartbeat(Protocol.Tcp, 3f));
                }
                OnNeutronConnected.Invoke(IsConnected, this);
            }
            else NeutronLogger.Print("Connection Refused!");
        }

        //* Metódo que ler os dados recebidos do servidor.
        private async void OnReceiveData(Protocol nProtocol, object nToken)
        {
            try
            {
                byte[] header = new byte[sizeof(int)]; //* aqui será armazenado o pre-fixo do cabeçalho, que é o tamanho da mensagem enviada pelo servidor.
                CancellationToken token = (CancellationToken)nToken;
                NetworkStream netStream = TcpSocket.GetStream();

                while (!token.IsCancellationRequested)
                {
                    if (nProtocol == Protocol.Tcp)
                    {
                        if (await SocketHelper.ReadAsyncBytes(netStream, header, 0, sizeof(int), token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                        {
                            int size = BitConverter.ToInt32(header, 0); //* converte o buffer do pre-fixo de volta em inteiro.
                            if (size > 0)
                            {
                                byte[] message = new byte[size]; //* cria um buffer com o tamanho da mensagem/pre-fixo.
                                if (await SocketHelper.ReadAsyncBytes(netStream, message, 0, size, token))  //* ler a mensagem e armazena no buffer de mensagem.
                                {
                                    OnProcessData(message); //* Processa os dados recebidos.
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
                    else if (nProtocol == Protocol.Udp)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            var udpReceiveResult = await UdpSocket.ReceiveAsync();  //* Recebe os dados enviados pelo servidor.
                            if (udpReceiveResult.Buffer.Length > 0)
                            {
                                OnProcessData(udpReceiveResult.Buffer); //* Processa os dados recebidos.
                                {
                                    //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                    NeutronStatistics.m_ClientUDP.AddIncoming(udpReceiveResult.Buffer.Length);
                                }
                            }
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
            catch (Exception ex)
            {
                if (!NeutronLogger.LoggerError("OnReceiveData exception!"))
                    NeutronLogger.StackTrace(ex);
            }
        }

        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        private void OnProcessData(byte[] nBuffer)
        {
            try
            {
                using (NeutronReader nReader = PooledNetworkReaders.Pull())
                {
                    nReader.SetBuffer(nBuffer);
                    switch (nReader.ReadPacket<SystemPacket>())
                    {
                        case SystemPacket.Heartbeat:
                            NeutronLogger.Print("Heartbeat test packet");
                            break;
                        case SystemPacket.Handshake:
                            {
                                #region Timer
                                nLastTime = nReader.ReadDouble();
                                nDiffTime = Math.Abs(nLastTime - CurrentTime);
                                #endregion

                                #region Reader
                                int nPort = nReader.ReadInt32();
                                Player nPlayer = nReader.ReadExactly<Player>();
                                Player[] nPlayers = nReader.ReadExactly<Player[]>();
                                #endregion

                                #region Udp
                                UDPEndPoint = new IPEndPoint(IPAddress.Parse(NeutronConfig.Settings.GlobalSettings.Address), nPort);
                                #endregion

                                #region Logic
                                HandleConnected(nPlayer);
                                foreach (var nIPlayer in nPlayers)
                                {
                                    nIPlayer.IsConnected = true;
                                    if (nIPlayer.Equals(MyPlayer))
                                        continue;
                                    else
                                        PlayerConnections[nIPlayer.ID] = nIPlayer;
                                }
                                #endregion
                            }
                            break;
                        case SystemPacket.NewPlayer:
                            {
                                #region Reader
                                Player nPlayer = nReader.ReadExactly<Player>();
                                #endregion

                                #region Logic
                                nPlayer.IsConnected = true;
                                PlayerConnections[nPlayer.ID] = nPlayer;
                                #endregion

                                #region Event
                                OnPlayerConnected.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.Disconnection:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                string nReason = nReader.ReadString();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                nPlayer.IsConnected = false;
                                #endregion

                                #region Event
                                OnPlayerDisconnected.Invoke(nReason, nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.Chat:
                            {
                                #region Reader
                                string nMessage = nReader.ReadString();
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                #endregion

                                #region Event
                                OnMessageReceived.Invoke(nMessage, nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.iRPC:
                            {
                                #region Reader
                                int nNetworkID = nReader.ReadInt32();
                                int nIRPCId = nReader.ReadInt32();
                                int nPlayerId = nReader.ReadInt32();
                                byte[] nParameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                iRPCHandler(nIRPCId, nNetworkID, nParameters, IsMine(nPlayer), nPlayer, new NeutronMessageInfo(0));
                                #endregion
                            }
                            break;
                        case SystemPacket.sRPC:
                            {
                                #region Reader
                                int nSRPCId = nReader.ReadInt32();
                                int nPlayerId = nReader.ReadInt32();
                                byte[] nParameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                sRPCHandler(nSRPCId, nPlayer, nParameters, false, IsMine(nPlayer));
                                #endregion
                            }
                            break;
                        case SystemPacket.GetChannels:
                            {
                                #region Reader
                                Channel[] nChannels = nReader.ReadExactly<Channel[]>();
                                #endregion

                                #region Logic
                                OnChannelsReceived.Invoke(nChannels, this);
                                #endregion
                            }
                            break;
                        case SystemPacket.JoinChannel:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                OnPlayerJoinedChannel.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.Leave:
                            {
                                #region Reader
                                MatchmakingPacket nMatchmakingPacket = nReader.ReadPacket<MatchmakingPacket>();
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                if (nMatchmakingPacket == MatchmakingPacket.Channel)
                                    OnPlayerLeftChannel.Invoke(nPlayer, IsMine(nPlayer), this);
                                else if (nMatchmakingPacket == MatchmakingPacket.Room)
                                    OnPlayerLeftRoom.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.Fail:
                            {
                                #region Reader
                                SystemPacket nSystemPacket = nReader.ReadPacket<SystemPacket>();
                                string nMessage = nReader.ReadString();
                                #endregion

                                #region Logic
                                NeutronLogger.LoggerError($"[{nSystemPacket}] -> | ERROR | {nMessage}");
                                #endregion

                                #region Event
                                OnFail.Invoke(nSystemPacket, nMessage, this);
                                #endregion
                            }
                            break;
                        case SystemPacket.CreateRoom:
                            {
                                #region Reader
                                Room nRoom = nReader.ReadExactly<Room>();
                                #endregion

                                #region Logic
                                OnPlayerCreatedRoom.Invoke(nRoom, this);
                                #endregion
                            }
                            break;
                        case SystemPacket.GetRooms:
                            {
                                #region Reader
                                Room[] nRooms = nReader.ReadExactly<Room[]>();
                                #endregion

                                #region Logic
                                OnRoomsReceived.Invoke(nRooms, this);
                                #endregion
                            }
                            break;
                        case SystemPacket.JoinRoom:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                OnPlayerJoinedRoom.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.DestroyPlayer:
                            {
                                #region Logic
                                OnPlayerDestroyed.Invoke(this);
                                #endregion
                            }
                            break;
                        case SystemPacket.Nickname:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                OnPlayerNicknameChanged.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.SetPlayerProperties:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                OnPlayerPropertiesChanged.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.SetRoomProperties:
                            {
                                #region Reader
                                int nPlayerId = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                OnRoomPropertiesChanged.Invoke(nPlayer, IsMine(nPlayer), this);
                                #endregion
                            }
                            break;
                        case SystemPacket.ClientPacket:
                            {
                                #region Reader
                                ClientPacket nClientPacket = nReader.ReadPacket<ClientPacket>();
                                int nPlayerId = nReader.ReadInt32();
                                byte[] nParameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                Player nPlayer = PlayerConnections[nPlayerId];
                                using (var PoolReader = PooledNetworkReaders.Pull())
                                {
                                    PoolReader.SetBuffer(nParameters);
                                    OnPlayerPacketReceived.Invoke(PoolReader, nPlayer, nClientPacket, this);
                                }
                                #endregion
                            }
                            break;
                    }
                }
            }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); }
        }

        #region Packets
        //* Inicia uma pulsação para notificar que o cliente ainda está ativo.
        //* Se o servidor parar de receber esta pulsação o cliente será desconectado.
        private IEnumerator Heartbeat(Protocol nProtocol, float nDelay)
        {
            while (!_cts.Token.IsCancellationRequested)
            {
                using (NeutronWriter nWriter = PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0); // * Reseta o escritor, apaga os dados antigos.
                    nWriter.WritePacket(SystemPacket.Heartbeat);
                    nWriter.Write(CurrentTime);
                    Send(nWriter.ToArray(), nProtocol);
                }
                yield return new WaitForSeconds(nDelay);
            }
        }

        /// <summary>
        ///* Sai da Sala, Canal ou Grupo.<br/>
        ///* A saída falhará se você não estiver em um canal, sala ou grupo.<br/>
        ///* Retorno de chamada: OnPlayerLeftChannel, OnPlayerLeftRoom, OnPlayerLeftGroup ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="nMatchmakingPacket">* O tipo do pacote de saída.</param>
        public void Leave(MatchmakingPacket nMatchmakingPacket)
        {
            using (NeutronWriter nWriter = PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.WritePacket(SystemPacket.Leave);
                nWriter.WritePacket(nMatchmakingPacket);
                Send(nWriter.ToArray());
            }
        }

        /// <summary>
        ///* Envia uma mensagem de Chat para o túnel especificado.<br/>
        ///* O envio falhará se a mensagem for em branco, nulo.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="nMessage">* A mensagem que será enviada.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        public void SendMessage(string nMessage, Broadcast nBroadcast)
        {
            using (NeutronWriter nWriter = PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.WritePacket(SystemPacket.Chat);
                nWriter.WritePacket(ChatPacket.Global);
                nWriter.WritePacket(nBroadcast);
                nWriter.Write(nMessage);
                Send(nWriter.ToArray());
            }
        }

        /// <summary>
        ///* Envia uma mensagem privada de chat para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir ou se a mensagem for nula, em branco.<br/>
        ///* Retorno de chamada: OnMessageReceived ou OnFail.<br/> 
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="nMessage">* A mensagem que será enviada.</param>
        /// <param name="nPlayer">* O jogador de destino da mensagem.</param>
        public void SendMessage(string nMessage, Player nPlayer)
        {
            using (NeutronWriter nWriter = PooledNetworkWriters.Pull())
            {
                nWriter.SetLength(0);
                nWriter.WritePacket(SystemPacket.Chat);
                nWriter.WritePacket(ChatPacket.Private);
                nWriter.Write(nPlayer.ID);
                nWriter.Write(nMessage);
                Send(nWriter.ToArray());
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para a rede.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="nWriter">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="nClientPacket">* O Pacote personalizado que será usado.</param>
        /// <param name="nSendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="nBroadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="nRecProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="nSendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter nWriter, ClientPacket nClientPacket, SendTo nSendTo, Broadcast nBroadcast, Protocol nRecProtocol, Protocol nSendProtocol)
        {
            using (NeutronWriter nIWriter = Neutron.PooledNetworkWriters.Pull())
            {
                nIWriter.SetLength(0);
                nIWriter.WritePacket(SystemPacket.ClientPacket);
                nIWriter.Write(MyPlayer.ID);
                nIWriter.WritePacket(nClientPacket);
                nIWriter.WritePacket(nSendTo);
                nIWriter.WritePacket(nBroadcast);
                nIWriter.WritePacket(nRecProtocol);
                nIWriter.WriteExactly(nWriter.ToArray());
                Send(nIWriter.ToArray(), nSendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="paramsWriter">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="player">* O jogador de destino do pacote.</param>
        /// <param name="clientPacket">* O Pacote personalizado que será usado.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter paramsWriter, Player player, ClientPacket clientPacket, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.ClientPacket);
                writer.Write(player.ID);
                writer.WritePacket(clientPacket);
                writer.WritePacket(recProtocol);
                writer.WriteExactly(paramsWriter.ToArray());
                Send(writer.ToArray(), sendProtocol);
            }
        }

        /// <summary>
        ///* Envia um pacote personalizado para um jogador específico utilizando seu objeto de rede.<br/>
        ///* O envio falhará se o jogador especificado não existir.<br/>
        ///* Retorno de chamada: OnPlayerPacketReceived ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="paramsWriter">* Os parâmetros que o pacote irá enviar.</param>
        /// <param name="view">* O jogador de destino do pacote.</param>
        /// <param name="clientPacket">* O Pacote personalizado que será usado.</param>
        /// <param name="recProtocol">* O protocolo que será usado para receber o pacote.</param>
        /// <param name="sendProtocol">* O protocolo que será usado para enviar o pacote.</param>
        public void Send(NeutronWriter paramsWriter, NeutronView view, ClientPacket clientPacket, Protocol recProtocol, Protocol sendProtocol)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.ClientPacket);
                writer.Write(view.ID);
                writer.WritePacket(clientPacket);
                writer.WritePacket(recProtocol);
                writer.WriteExactly(paramsWriter.ToArray());
                Send(writer.ToArray(), sendProtocol);
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
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.Nickname);
                writer.Write(nickname);
                Send(writer.ToArray());
            }
            m_Nickname = nickname;
        }

        /// <summary>
        ///* Ingressa em um canal pelo ID.<br/>
        ///* Se o ID for 0, ingressará em um canal aleatório.<br/>
        ///* A entrada em um canal falhará se o canal estiver cheio, fechado, não existente ou quando o usuário já estiver presente no canal.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="ChannelID">* O ID do canal que deseja ingressar.</param>
        public void JoinChannel(int ChannelID)
        {
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.JoinChannel);
                writer.Write(ChannelID);
                Send(writer.ToArray());
            }
        }

        /// <summary>
        ///* Ingressa em uma sala pelo ID.<br/>
        ///* Se o ID for 0, ingressará em uma sala aleatória.<br/>
        ///* A entrada em uma sala falhará se a sala estiver cheia, fechada, não existente ou quando o usuário já estiver presente na sala.<br/>
        ///* Retorno de chamada: OnPlayerJoinedRoom ou OnFail.<br/>
        ///* Para mais detalhes, consulte a documentação.
        /// </summary>
        /// <param name="RoomID">* O ID da sala que deseja ingressar.</param>
        public void JoinRoom(int RoomID)
        {
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.JoinRoom);
                writer.Write(RoomID);
                Send(writer.ToArray());
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
        /// <param name="visible">* Define se a sala é visivel em lobby.</param>
        /// <param name="JoinOrCreate">* Define se deve criar uma sala nova ou ingressar se uma sala com o mesmo nome já existe.</param>
        public void CreateRoom(string roomName, int maxPlayers, string password, Dictionary<string, object> properties, bool visible = true, bool JoinOrCreate = false)
        {
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.CreateRoom);
                writer.Write(roomName);
                writer.Write(maxPlayers);
                writer.Write(password ?? string.Empty);
                writer.Write(visible);
                writer.Write(JoinOrCreate);
                writer.Write(JsonConvert.SerializeObject(properties));
                Send(writer.ToArray());
            }
        }

        /// <summary>
        ///* Obtém os pacotes armazenados em cache.<br/>
        ///* Falhará se o ID especificado não for válido ou se não existir pacotes em cache.<br/>
        ///* Retorno de chamada: Nenhum.<br/>
        ///* Para mais detalhes, consulte a documentação. 
        /// </summary>
        /// <param name="packet">* O tipo de pacote que deseja obter.</param>
        /// <param name="ID">* ID do pacote que deseja obter os dados.</param>
        /// <param name="includeMe">* Define se você deve receber pacotes em cache que são seus.</param>
        public void GetCachedPackets(CachedPacket packet, int ID, bool includeMe = true)
        {
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.GetChached);
                writer.WritePacket(packet);
                writer.Write(ID);
                writer.Write(includeMe);
                Send(writer.ToArray());
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
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.GetChannels);
                Send(writer.ToArray());
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
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.GetRooms);
                Send(writer.ToArray());
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
            string _properties = JsonConvert.SerializeObject(properties);
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.SetPlayerProperties);
                writer.Write(_properties);
                Send(writer.ToArray());
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
            string _properties = JsonConvert.SerializeObject(properties);
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.SetRoomProperties);
                writer.Write(_properties);
                Send(writer.ToArray());
            }
        }
        #endregion

        /// <summary>
        ///* Obtém as informações da sua conexão com o servidor.
        ///* Para mais detalhes, consulte a documentação.<br/> 
        /// </summary>
        /// <param name="Ping">* A latência entre você e o servidor.</param>
        /// <param name="PcktLoss">* A quantidade de pacotes perdidos.</param>
        /// <param name="iterations">* A quantidade de testes que será realizado.</param>
        public void GetNetworkStats(out long Ping, out double PcktLoss, int iterations)
        {
            #region Sender
            for (int i = 0; i < iterations; i++)
            {
                m_PingAmount++;
                using (System.Net.NetworkInformation.Ping pingSender = new System.Net.NetworkInformation.Ping())
                {
                    pingSender.PingCompleted += (object sender, PingCompletedEventArgs e) =>
                    {
                        if (e.Reply != null)
                        {
                            if (e.Reply.Status == IPStatus.Success)
                                m_Ping = e.Reply.RoundtripTime;
                            else m_PacketLoss += 1;
                        }
                    };
                    pingSender.SendAsync(NeutronConfig.Settings.GlobalSettings.Address, null);
                }
            }
            #endregion
            Ping = m_Ping;
            PcktLoss = (m_PacketLoss / m_PingAmount) * 100; //* Packet loss em porcentagem.

            //* Reseta os status.
            #region Reset
            m_Ping = 0;
            m_PacketLoss = 0;
            m_PingAmount = 0;
            #endregion
        }

        /// <summary>
        ///* Retorna se o jogador especificado é seu.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="mPlayer">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(Player mPlayer)
        {
            return mPlayer.Equals(MyPlayer);
        }

        /// <summary>
        ///* Retorna se você é o dono(master) da sala.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <returns></returns>
        public bool IsMasterClient()
        {
            return CurrentRoom.Owner.Equals(MyPlayer);
        }

        /// <summary>
        ///* Envia uma chamada(sRPC) que aciona a criação do seu jogador.<br/>
        ///* ID: 1001<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="options">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreatePlayer(NeutronWriter options)
        {
            sRPC(MyPlayer.ID, 1001, options, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(sRPC) que aciona a criação de um objeto.<br/>
        ///* ID: 1002<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="options">* Os parâmetros que serão enviados com sua chamada.</param>
        public void CreateObject(NeutronWriter options)
        {
            sRPC(MyPlayer.ID, 1002, options, Protocol.Tcp);
        }

        /// <summary>
        ///* Destrói seu objeto de rede que representa seu jogador.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        public void DestroyPlayer()
        {
            using (var writer = PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(SystemPacket.DestroyPlayer);
                Send(writer.ToArray());
            }
        }

        /// <summary>
        ///* Usado para a comunicação entre: Client->Server | Server->Client.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="nID">* ID do objeto de rede que será usado para transmitir os dados.</param>
        /// <param name="nonDynamicID">* ID do metódo que será invocado.</param>
        /// <param name="writer">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="protocolType">* O protocolo que será usado para enviar os dados.</param>
        public void sRPC(int nID, int nonDynamicID, NeutronWriter writer, Protocol protocolType)
        {
            InternalRCC(nID, nonDynamicID, writer.ToArray(), protocolType);
        }

        /// <summary>
        ///* Usado para a comunicação entre: Client->Server | Server->Client.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="nID">* ID do objeto de rede que será usado para identificar a instância que deve invocar o metódo.</param>
        /// <param name="dynamicID">* ID do metódo que será invocado.</param>
        /// <param name="parametersStream">* Os parâmetros que serão enviados para o metódo a ser invocado.</param>
        /// <param name="cacheMode">* O Tipo de armazenamento em cache que será usado para guardar em cache.</param>
        /// <param name="sendTo">* Define quais jogadores devem ser incluídos na lista de recepção do pacote.</param>
        /// <param name="broadcast">* O Túnel que será usado para a transmissão.</param>
        /// <param name="protocolType">* O protocolo que será usado para enviar os dados.</param>
        public void iRPC(int nID, int dynamicID, NeutronWriter parametersStream, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            InternalRPC(nID, dynamicID, parametersStream.ToArray(), cacheMode, sendTo, broadcast, protocolType);
        }

        /// <summary>
        ///* Cria uma instância de Neutron que será usada para realizar a conexão e a comunicação com o servidor.<br/>
        ///* As instâncias são independentes, cada instância é uma conexão nova.<br/>
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="type">* O tipo de cliente que a instância usará.</param>
        /// <param name="ownerGameObject">* O objeto que é dono dessa instância.</param>
        /// <returns>Retorna uma instância do tipo Neutron.</returns>
        public static Neutron CreateClient(ClientType type, GameObject ownerGameObject)
        {
            Neutron neutronInstance = ownerGameObject.AddComponent<Neutron>();
            neutronInstance.InitializeClient(type);
            if (type == ClientType.MainPlayer)
                Client = neutronInstance;
            return neutronInstance;
        }

        /// <summary>
        ///* Cria um objeto em rede.
        ///* Para mais detalhes, consulte a documentação.<br/>
        /// </summary>
        /// <param name="IsServer">* Define se o ambiente a ser instaciado é o servidor ou o cliente.</param>
        /// <param name="prefab">* O Prefab que será usado para criar o objeto em rede.</param>
        /// <param name="position">* A posição que o objeto usará no momento de sua criação.</param>
        /// <param name="rotation">* A rotação que o objeto usará no momento de sua criação.</param>
        /// <returns>Retorna uma instância do tipo NeutronView.</returns>
        public static NeutronView Spawn(bool IsServer, GameObject prefab, Vector3 position, Quaternion rotation)
        {
            #region Local Functions
            NeutronView Spawn() => Instantiate(prefab, position, rotation).GetComponent<NeutronView>();
            #endregion

            #region Logic
            if (prefab.TryGetComponent<NeutronView>(out NeutronView neutronView))
            {
                switch (neutronView.Ambient)
                {
                    case Ambient.Both:
                        return Spawn();
                    case Ambient.Server:
                        return IsServer ? Spawn() : null;
                    case Ambient.Client:
                        return !IsServer ? Spawn() : null;
                    default:
                        return null;
                }
            }
            else if (!NeutronLogger.LoggerError("\"Neutron View\" object not found, failed to instantiate in network."))
                return null;
            else return null;
            #endregion
        }
    }
}