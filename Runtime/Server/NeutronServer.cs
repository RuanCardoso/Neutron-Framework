using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using System.Net;
using NeutronNetwork.Extensions;
using System.IO;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Wrappers;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Internal;
using System.Text;
using NeutronNetwork.UI;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
///* Criado por: Ruan Cardoso(Brasil)
///* Os br também são pica.
///* Email: cardoso.ruan050322@gmail.com
///* Licença: GNU AFFERO GENERAL PUBLIC LICENSE
/// </summary>
namespace NeutronNetwork.Server
{
    ///* Esta classe é o núcleo do Neutron, aqui é o lado do servidor, você pode fazer oque quiser.
    ///* Desde que saiba oque está fazendo skksaksaksak, eu não sei, aqui é só sorte e paciência.
    ///* Um salve pra Unity Brasil.
    [RequireComponent(typeof(NeutronModule))]
    [RequireComponent(typeof(NeutronSchedule))]
    [RequireComponent(typeof(NeutronFramerate))]
    [RequireComponent(typeof(NeutronStatistics))]
    [RequireComponent(typeof(NeutronUI))]
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : ServerBase
    {
        public static StringBuilder filter_tcp_udp_client_server = new StringBuilder();
        public static StringBuilder filter_udp_client_server = new StringBuilder();
        public static StringBuilder filter_tcp_client_server = new StringBuilder();
        public static StringBuilder filter_tcp_client = new StringBuilder();
        public static StringBuilder filter_tcp_server = new StringBuilder();
        public static StringBuilder filter_udp_client = new StringBuilder();
        public static StringBuilder filter_udp_server = new StringBuilder();

        #region Events
        /// <summary>
        ///* Este evento é acionando quando o servidor é iniciado.
        /// </summary>
        public static NeutronEventNoReturn OnStart {
            get;
            set;
        }
        /// <summary>
        ///* Este evento é acionado quando um jogador é conectado ao servidor.
        /// </summary>
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerConnected {
            get; set;
        }
        #endregion

        #region Properties
        /// <summary>
        ///* Obtém o status do servidor.
        /// </summary>
        public static bool Initialized {
            get;
            set;
        }
        /// <summary>
        ///* Objeto de jogador que representa o servidor.
        /// </summary>
        public NeutronPlayer Player {
            get;
            set;
        }
        /// <summary>
        ///* Intância de Neutron do Servidor.
        /// </summary>
        public Neutron Instance {
            get;
            set;
        }
        #endregion

        #region Fields -> Collections
        //* Esta fila irá armazenar os clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<TcpClient> _acceptedClients = new NeutronBlockingQueue<TcpClient>(/*NeutronConstantsSettings.BOUNDED_CAPACITY*/);
        //* Esta fila irá armazenar os pacotes recebidos dos clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<NeutronPacket> _dataForProcessing = new NeutronBlockingQueue<NeutronPacket>(/*NeutronConstantsSettings.BOUNDED_CAPACITY*/);
        //* Esta fila irá fornecer um pool de ID's e atribuirá um ID único para cada cliente novo.
        //* Quando um cliente é desconectado seu ID é reutilizado sendo adicionado a fila novamente.
        public NeutronSafeQueueNonAlloc<int> _pooledIds = new NeutronSafeQueueNonAlloc<int>(0);
        #endregion

        #region Threading
        //* Este é um token de cancelamento, ele é passado para todos os Threads, é usado para parar os Threads quando o servidor é desligado.
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        #region Functions
        private void Initilize()
        {
            Player = PlayerHelper.MakeTheServerPlayer();
            //* Faz a porra da instância de neutron, gambiarra, eu errei o design do "Neutron" essa porra tá cheio de confusões e gambiarras, tou entendo mais nada, uma falha de Design ):
            Instance = new Neutron(Player, true, Instance);
            //* Inicializa a porra toda. gambiarra, pq sou noob ):
            Instance.Initialize(Instance);
            //* Esta região irá fornecer os ID para a lista.
            #region Provider
            for (int i = 0; i < NeutronModule.Settings.GlobalSettings.MaxPlayers; i++)
                _pooledIds.Enqueue((NeutronConstantsSettings.GENERATE_PLAYER_ID + i) + 1);
            #endregion

            //* Marca o servidor como inicializado.
            Initialized = true;

            #region Logger
            LogHelper.Info("The server is ready, all protocols(TCP, UDP, RUDP) have been initialized.\r\n");
            #endregion

            ///* 3 threads geral para o servidor, não é criado um thread novo por conexão, por uma questão de desempenho, em vez disso, eu utilizo o ThreadPool.
            //* Este thread será dedicado a aceitar e enfileirar os novos clientes.
            #region Threads
            Thread acptTh = new Thread((t) => OnAcceptedClient())
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                IsBackground = true,
                Name = "Neutron acptTh"
            };
            acptTh.Start();

            //* Este thread será dedicado a desinfileirar os pacotes e processa-los.
            Thread packetProcessingStackTh = new Thread((e) => PacketProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Normal,
                IsBackground = true,
                Name = "Neutron packetProcessingStackTh"
            };
            packetProcessingStackTh.Start();

            //* Este thread será dedicado a desinfileirar os novos clientes e processa-los.
            Thread clientsProcessingStackTh = new Thread((e) => ClientsProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Lowest,
                IsBackground = true,
                Name = "Neutron ClientsProcessingStackTh"
            };
            clientsProcessingStackTh.Start();
            #endregion

            #region Events
            OnStart?.Invoke();
            #endregion
        }

        //* Aceita os clientes e os adiciona a fila.
        private async void OnAcceptedClient()
        {
            CancellationToken token = TokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client = await TcpListener.AcceptTcpClientAsync(); //* Aceita a nova conexão,
                    _acceptedClients.Add(client, token); //* Nova conexão é enfileirada para processamento.
                }
                catch (ObjectDisposedException) { continue; }
                catch (OperationCanceledException) { continue; }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                    continue;
                }
            }
        }

        //* Inicia o processamento dos clientes.
        private void ClientsProcessingStack()
        {
            CancellationToken token = TokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    TcpClient tcpClient = _acceptedClients.Take(token); //* Desinfileira os clientes e bloqueia o thread se não houver mais dados.
                    if (PlayerHelper.GetAvailableID(out int ID))
                    {
                        if (SocketHelper.AllowConnection(tcpClient)) //* Verifica se este cliente antigiu o limite de conexões ativas.
                        {
                            tcpClient.NoDelay = Helper.GetSettings().GlobalSettings.NoDelay;
                            tcpClient.ReceiveBufferSize = Helper.GetConstants().Tcp.TcpReceiveBufferSize;
                            tcpClient.SendBufferSize = Helper.GetConstants().Tcp.TcpSendBufferSize;
                            // TODO tcpClient.ReceiveTimeout = int.MaxValue; // only synchronous.
                            // TODO tcpClient.SendTimeout = int.MaxValue; // only synchronous.
                            var player = new NeutronPlayer(ID, tcpClient, new CancellationTokenSource()); //* Cria uma instância do cliente.
                            if (SocketHelper.AddPlayer(player))
                            {
                                //* Incrementa a quantidade de jogadores do servidor.
                                Interlocked.Increment(ref _playerCount);
                                //* Esta região cria um View, um View é usado para você criar uma comunicação personalizada com o cliente dono(owner).
                                //* Exemplo, dentro do View você pode implementar uma função que envia um evento ou mensagem a cada X Segundos.
                                #region View
                                NeutronSchedule.ScheduleTask(() =>
                                {
                                    GameObject playerGlobalController = GameObject.Instantiate(PlayerGlobalController.gameObject);
                                    PlayerGlobalController.hideFlags = HideFlags.HideInHierarchy;
                                    playerGlobalController.name = $"Player Global Controller[{player.ID}]";
                                    foreach (Component component in playerGlobalController.GetComponents<Component>())
                                    {
                                        Type type = component.GetType();
                                        if (type.BaseType != typeof(PlayerGlobalController) && type != typeof(Transform))
                                            GameObject.Destroy(component);
                                        else
                                        {
                                            if (type.BaseType == typeof(PlayerGlobalController))
                                            {
                                                var controller = (PlayerGlobalController)component;
                                                controller.Player = player;
                                            }
                                        }
                                    }
                                    //* Move o Player Actions para o container do servidor.
                                    SceneHelper.MoveToContainer(playerGlobalController, "[Container] -> Server");
                                });
                                #endregion

                                IPEndPoint tcpRemote = (IPEndPoint)player.TcpClient.Client.RemoteEndPoint;
                                IPEndPoint tcpLocal = (IPEndPoint)player.TcpClient.Client.LocalEndPoint;
                                IPEndPoint udpLocal = (IPEndPoint)player.UdpClient.Client.LocalEndPoint;
                                //***************************************************************************************************************************************************************************************************************************************************************
                                LogHelper.Info($"\r\nIncoming Client -> Ip: [{tcpRemote.Address.ToString().Bold()}] & Port: [Tcp: {tcpRemote.Port.ToString().Bold()} | Udp: {tcpRemote.Port.ToString().Bold()} - {udpLocal.Port.ToString().Bold()}]\r\n");
#if UNITY_EDITOR
                                LogHelper.Info("\r\nFilters(Server->Client): ".Italic() + $"(tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port})".Bold().Color("yellow") + "\r\n");
                                LogHelper.Info("\r\nFilters(Client->Server): ".Italic() + $"(tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})".Bold().Color("yellow") + "\r\n");
                                LogHelper.Info("\r\nFilters(Client->Server->Client): ".Italic() + $"((tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})) or ((tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port}))".Bold().Color("yellow") + "\r\n");
                                //***************************************************************************************************************************************************************************************************************************************************************
                                filter_tcp_udp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((tcp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {tcpRemote.Port})) or ((tcp.SrcPort == {tcpLocal.Port}) or (udp.SrcPort == {udpLocal.Port}))");
                                filter_udp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((udp.SrcPort == {tcpRemote.Port}) or (udp.SrcPort == {udpLocal.Port}))");
                                filter_tcp_client_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}((tcp.SrcPort == {tcpRemote.Port}) or (tcp.SrcPort == {tcpLocal.Port}))");
                                filter_tcp_client.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(tcp.SrcPort == {tcpRemote.Port})");
                                filter_tcp_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(tcp.SrcPort == {tcpLocal.Port})");
                                filter_udp_client.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(udp.SrcPort == {tcpRemote.Port})");
                                filter_udp_server.Append($"\r\n{((_playerCount > 1) ? " or " : string.Empty)}(udp.SrcPort == {udpLocal.Port})");
#endif
                                //* Usa o pool de threads para receber os dados do cliente de forma assíncrona.
                                //* O Pool de threads só deverá ser usado com metódos que não bloqueiam, para evitar gargalos no recebimento de dados.
                                //* Utilizando o pool de Threads, em vez de criar um thread novo para cada conexão.
                                switch (Helper.GetConstants().ReceiveThread)
                                {
                                    case ThreadType.Neutron:
                                        {
                                            ThreadPool.QueueUserWorkItem((e) =>
                                            {
                                                OnReceivingData(player.NetworkStream, player, Protocol.Tcp);
                                                OnReceivingData(player.NetworkStream, player, Protocol.Udp);
                                            });
                                            break;
                                        }
                                    case ThreadType.Unity:
                                        NeutronSchedule.ScheduleTask(() =>
                                        {
                                            OnReceivingData(player.NetworkStream, player, Protocol.Tcp);
                                            OnReceivingData(player.NetworkStream, player, Protocol.Udp);
                                        });
                                        break;
                                }
                                OnPlayerConnected?.Invoke(player);
                            }
                            else
                            {
                                if (!LogHelper.Error("Failed to add Player!"))
                                    player.Dispose();
                                continue;
                            }
                        }
                        else
                        {
                            if (!LogHelper.Error("Client not allowed!"))
                                tcpClient.Close();
                            continue;
                        }
                    }
                    else
                    {
                        if (!LogHelper.Error("Max players reached!"))
                            tcpClient.Close();
                        continue;
                    }
                }
                catch (ObjectDisposedException) { continue; }
                catch (OperationCanceledException) { continue; }
                catch (ArgumentNullException) { continue; }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                    continue;
                }
            }
        }

        //* Inicia o processamento dos pacotes.
        private void PacketProcessingStack()
        {
            CancellationToken token = TokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    NeutronPacket packet = _dataForProcessing.Take(token); //* Desinfileira os dados e bloqueia o thread se não houver mais dados.
                    switch (Helper.GetConstants().PacketThread)
                    {
                        case ThreadType.Neutron:
                            RunPacket(packet);
                            break;
                        case ThreadType.Unity:
                            {
                                NeutronSchedule.ScheduleTask(() =>
                                {
                                    RunPacket(packet);
                                });
                            }
                            break;
                    }
                }
                catch (ObjectDisposedException) { continue; }
                catch (OperationCanceledException) { continue; }
                catch (ArgumentNullException) { continue; }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                    continue;
                }
            }
        }

        //* Aqui os dados são enviados aos seus clientes.
        public void OnSendingData(NeutronPlayer player, NeutronPacket neutronPacket)
        {
            try
            {
                using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                {
                    var playerId = (short)neutronPacket.Sender.ID;
                    byte[] pBuffer = neutronPacket.Buffer.Compress();
                    switch (neutronPacket.Protocol)
                    {
                        case Protocol.Tcp:
                            {
                                NeutronStream.IWriter wHeader = stream.Writer;
                                wHeader.WriteByteArrayWithAutoSize(pBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho e a mensagem.
                                wHeader.Write(playerId); //* Pre-fixa o id do jogador no cabeçalho, um short(2 bytes).
                                byte[] hBuffer = wHeader.ToArray();
                                wHeader.Write();

                                NetworkStream networkStream = player.TcpClient.GetStream();
                                switch (Helper.GetConstants().SendModel)
                                {
                                    case SendType.Synchronous:
                                        networkStream.Write(hBuffer, 0, hBuffer.Length);
                                        break;
                                    default:
                                        if (Helper.GetConstants().SendAsyncPattern == AsynchronousType.APM)
                                            networkStream.Write(hBuffer, 0, hBuffer.Length);
                                        else
                                            SocketHelper.SendTcpAsync(networkStream, hBuffer, player.TokenSource.Token);
                                        break;
                                }
                                //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                NeutronStatistics.ServerTCP.AddOutgoing(hBuffer.Length);
                            }
                            break;
                        case Protocol.Udp:
                            {
                                NeutronStream.IWriter wHeader = stream.Writer;
                                wHeader.Write(playerId); //* Pre-fixa o id do jogador no cabeçalho, um short(2 bytes).
                                wHeader.WriteNext(pBuffer);
                                byte[] hBuffer = wHeader.ToArray();
                                wHeader.Write();

                                player.StateObject.SendDatagram = hBuffer;
                                if (player.StateObject.UdpIsReady()) //* Verifica se o IP de destino não é nulo, se sim, o servidor não enviará os dados.
                                {
                                    NonAllocEndPoint remoteEp = (NonAllocEndPoint)player.StateObject.NonAllocEndPoint;
                                    switch (Helper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            SocketHelper.SendBytes(player.UdpClient, hBuffer, remoteEp);
                                            break;
                                        default:
                                            {
                                                switch (Helper.GetConstants().SendAsyncPattern)
                                                {
                                                    case AsynchronousType.APM:
                                                        {
                                                            SocketHelper.BeginSendBytes(player.UdpClient, hBuffer, remoteEp, (ar) =>
                                                            {
                                                                SocketHelper.EndSendBytes(player.UdpClient, ar);
                                                            }); //* Envia os dados pro cliente.
                                                            break;
                                                        }

                                                    default:
                                                        SocketHelper.SendUdpAsync(player.UdpClient, player.StateObject, remoteEp);
                                                        break;
                                                }
                                                break;
                                            }
                                    }
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ServerUDP.AddOutgoing(hBuffer.Length);
                                }
                                else
                                    LogHelper.Error($"{player.ID} Udp Endpoint is null!");
                            }
                            break;
                    }
                }
            }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception) { }
        }

        //* Adiciona um pacote, meio que "simula" o recebimento de um pacote do cliente ou de algum cliente.
        public void AddPacket(NeutronPacket packet)
        {
            lock (Encapsulate.BeginLock)
            {
                if (Encapsulate.Sender != null)
                    packet.Sender = Encapsulate.Sender;
                packet.IsServerSide = true;
                _dataForProcessing.Add(packet);
            }
        }

        private void CreateUdpPacket(NeutronPlayer player)
        {
            byte[] datagram = player.StateObject.ReceivedDatagram;
            //* descomprime a porra do pacote.
            byte[] pBuffer = datagram.Decompress();
            NeutronPacket neutronPacket = Helper.PollPacket(pBuffer, player, player, Protocol.Udp);
            _dataForProcessing.Add(neutronPacket, player.TokenSource.Token); //* Adiciona os dados na fila para processamento.
            //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
            NeutronStatistics.ServerUDP.AddIncoming(datagram.Length);
        }

        private void UdpApmReceive(NeutronPlayer player)
        {
            if (player.TokenSource.Token.IsCancellationRequested)
                return;

            SocketHelper.BeginReadBytes(player.UdpClient, player.StateObject, (ar) =>
            {
                EndPoint remoteEp = player.StateObject.NonAllocEndPoint;
                int bytesRead = SocketHelper.EndReadBytes(player.UdpClient, ref remoteEp, ar);
                //* Esta região funciona como um "Syn/Ack", o cliente envia algum pacote vazio após a conexão, após o servidor receber este pacote, atribui o ip de destino, que é para onde os dados serão enviados.
                //! Se o ip de destino é nulo, o servidor não enviará os dados, porque não tem destino, não houve "Syn/Ack".
                //! A tentativa de envio sem o "Syn/Ack" causará a exceção de "An existing connection was forcibly closed by the remote host"
                if (!player.StateObject.UdpIsReady())
                    player.StateObject.UdpRemoteEndPoint = (IPEndPoint)remoteEp;
                if (bytesRead > 0)
                {
                    player.StateObject.ReceivedDatagram = new byte[bytesRead];
                    Buffer.BlockCopy(player.StateObject.Buffer, 0, player.StateObject.ReceivedDatagram, 0, bytesRead);
                    CreateUdpPacket(player);
                }
                UdpApmReceive(player);
            });
        }

        //* Recebe/Ler os dados dos clientes.
        private async void OnReceivingData(Stream networkStream, NeutronPlayer player, Protocol protocol)
        {
            CancellationToken token = player.TokenSource.Token;
            try
            {
                bool whileOn = false;
                byte[] hBuffer = new byte[NeutronModule.HeaderSize]; //* aqui será armazenado o pre-fixo(tamanho/length) do pacote, que é o tamanho da mensagem transmitida
                while ((!TokenSource.Token.IsCancellationRequested && !token.IsCancellationRequested) && !whileOn) // Interrompe o loop em caso de cancelamento do Token, o cancelamento ocorre em desconexões ou exceções.
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                if (await SocketHelper.ReadAsyncBytes(networkStream, hBuffer, 0, NeutronModule.HeaderSize, token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                                {
                                    int size = ByteHelper.ReadSize(hBuffer); //* converte o buffer em inteiro.
                                    if (size > Helper.GetConstants().Tcp.MaxTcpPacketSize || size <= 0) //* Verifica se o tamanho da mensagem é válido.
                                    {
                                        if (!LogHelper.Error($"Invalid tcp message size! size: {size}"))
                                            DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                                    }
                                    else
                                    {
                                        byte[] packetBuffer = new byte[size]; //* cria um buffer com o tamanho da mensagem/pre-fixo.
                                        if (await SocketHelper.ReadAsyncBytes(networkStream, packetBuffer, 0, size, token)) //* ler a mensagem e armazena no buffer de mensagem.
                                        {
                                            packetBuffer = packetBuffer.Decompress();
                                            NeutronPacket neutronPacket = Helper.PollPacket(packetBuffer, player, player, Protocol.Tcp);
                                            //* Adiciona os dados na fila para processamento.
                                            _dataForProcessing.Add(neutronPacket, token);
                                            //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                            NeutronStatistics.ServerTCP.AddIncoming(size + hBuffer.Length);
                                        }
                                        else
                                            DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                                    }
                                }
                                else
                                    DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                            }
                            break;
                        case Protocol.Udp:
                            {
                                //* precisa nem dizer nada aqui né?
                                switch (Helper.GetConstants().ReceiveAsyncPattern)
                                {
                                    case AsynchronousType.TAP:
                                        if (await SocketHelper.ReadAsyncBytes(player.UdpClient, player.StateObject))
                                            CreateUdpPacket(player);
                                        break;
                                    default:
                                        UdpApmReceive(player);
                                        whileOn = true;
                                        break;
                                }
                            }
                            break;
                    }
                }
            }
            catch (SocketException) { }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
                if (!token.IsCancellationRequested)
                    DisconnectHandler(player);
            }
        }
        #endregion

        #region Packets
        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        private void RunPacket(NeutronPacket neutronPacket)
        {
            //* O buffer que armazena os dados recebido dos clientes.
            byte[] pBuffer = neutronPacket.Buffer;
            //* Define se o pacote foi simulado/enviado pelo servidor.
            bool isServer = neutronPacket.IsServerSide;
            //* O protocolo de transmissão do pacote.
            Protocol protocol = neutronPacket.Protocol;
            //* O jogador que criou o pacote.
            NeutronPlayer owner = neutronPacket.Owner;
            //* O jogador que enviou o pacote(mesmo do owner), o pacote pode ser criado por um jogador e "enviado" por outro, um remetente fake.
            NeutronPlayer sender = neutronPacket.Sender;
            //* Executa o pacote.
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader.SetBuffer(pBuffer);
                Packet outPacket = (Packet)reader.ReadPacket();
                if (OnReceivePacket(outPacket) || isServer)
                {
                    switch (outPacket) //* Ler o pacote recebido
                    {
                        // Test packet
                        case Packet.TcpKeepAlive:
                            {
                                using (NeutronStream tcpStream = Neutron.PooledNetworkStreams.Pull())
                                {
                                    NeutronStream.IWriter writer = tcpStream.Writer;
                                    writer.WritePacket((byte)Packet.TcpKeepAlive);
                                    owner.Write(writer);
                                }
                            }
                            break;
                        case Packet.Handshake:
                            {
                                #region Reader
                                string appId = reader.ReadString();
                                double time = reader.ReadDouble();
                                Authentication authentication = reader.ReadWithInteger<Authentication>();
                                #endregion

                                #region Logic
                                if (appId.Decrypt(out appId))
                                {
                                    if (Helper.GetSettings().GlobalSettings.AppId == appId)
                                        HandshakeHandler(owner, time, authentication);
                                    else
                                        owner.Error(Packet.Handshake, "Update your game version, it does not match the current server version.");
                                }
                                else if (!LogHelper.Error("Failed to verify handshake!"))
                                    DisconnectHandler(owner);
                                #endregion
                            }
                            break;
                        case Packet.Nickname:
                            {
                                #region Reader
                                string nickname = reader.ReadString();
                                #endregion

                                #region Logic
                                SetNicknameHandler(owner, nickname);
                                #endregion
                            }
                            break;
                        case Packet.Chat:
                            {
                                #region Defaults
                                TunnelingTo tunnelingTo = default(TunnelingTo);
                                int viewId = default(int);
                                #endregion

                                #region Reader
                                ChatMode chatPacket = (ChatMode)reader.ReadPacket();
                                switch (chatPacket)
                                {
                                    case ChatMode.Global:
                                        tunnelingTo = (TunnelingTo)reader.ReadPacket();
                                        break;
                                    case ChatMode.Private:
                                        viewId = reader.ReadInt();
                                        break;
                                }
                                string message = reader.ReadString();
                                #endregion

                                #region Logic
                                ChatHandler(owner, chatPacket, tunnelingTo, viewId, message);
                                #endregion
                            }
                            break;
                        case Packet.iRPC:
                            {
                                #region Reader
                                RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                                TargetTo targetTo = (TargetTo)reader.ReadPacket();
                                CacheMode cache = (CacheMode)reader.ReadPacket();
                                short viewId = reader.ReadShort();
                                byte rpcId = reader.ReadByte();
                                byte instanceId = reader.ReadByte();
                                byte[] buffer = reader.ReadNext();
                                #endregion

                                #region Logic
                                iRPCHandler(owner, sender, viewId, rpcId, instanceId, buffer, registerType, targetTo, cache, protocol);
                                #endregion
                            }
                            break;
                        case Packet.gRPC:
                            {
                                #region Reader
                                byte id = reader.ReadByte();
                                byte[] buffer = reader.ReadNext();
                                #endregion

                                #region Logic
                                gRPCHandler(owner, sender, id, buffer, protocol);
                                #endregion
                            }
                            break;
                        case Packet.GetChannels:
                            {
                                #region Logic
                                GetChannelsHandler(owner);
                                #endregion
                            }
                            break;
                        case Packet.JoinChannel:
                            {
                                #region Reader
                                int channelId = reader.ReadInt();
                                #endregion

                                #region Logic
                                JoinChannelHandler(owner, channelId);
                                #endregion
                            }
                            break;
                        case Packet.GetCache:
                            {
                                #region Reader
                                CachedPacket cachedPacket = (CachedPacket)reader.ReadPacket();
                                byte Id = reader.ReadByte();
                                bool includeMe = reader.ReadBool();
                                #endregion

                                #region Logic
                                GetCacheHandler(owner, cachedPacket, Id, includeMe);
                                #endregion
                            }
                            break;
                        case Packet.CreateRoom:
                            {
                                #region Reader
                                string password = reader.ReadString();
                                NeutronRoom room = reader.ReadWithInteger<NeutronRoom>();
                                #endregion

                                #region Logic
                                CreateRoomHandler(owner, room, password);
                                #endregion
                            }
                            break;
                        case Packet.GetRooms:
                            {
                                #region Logic
                                GetRoomsHandler(owner);
                                #endregion
                            }
                            break;
                        case Packet.JoinRoom:
                            {
                                #region Reader
                                int roomId = reader.ReadInt();
                                string password = reader.ReadString();
                                #endregion

                                #region Logic
                                JoinRoomHandler(owner, roomId, password);
                                #endregion
                            }
                            break;
                        case Packet.Leave:
                            {
                                #region Reader
                                MatchmakingMode packet = (MatchmakingMode)reader.ReadPacket();
                                #endregion

                                #region Logic
                                if (packet == MatchmakingMode.Room)
                                    LeaveRoomHandler(owner);
                                else if (packet == MatchmakingMode.Channel)
                                    LeaveChannelHandler(owner);
                                #endregion
                            }
                            break;
                        case Packet.Destroy:
                            {
                                #region Logic
                                DestroyPlayerHandler(owner);
                                #endregion
                            }
                            break;
                        case Packet.SetPlayerProperties:
                            {
                                #region Reader
                                string properties = reader.ReadString();
                                #endregion

                                #region Logic
                                SetPlayerPropertiesHandler(owner, properties);
                                #endregion
                            }
                            break;
                        case Packet.SetRoomProperties:
                            {
                                #region Reader
                                string properties = reader.ReadString();
                                #endregion

                                #region Logic
                                SetRoomPropertiesHandler(owner, properties);
                                #endregion
                            }
                            break;
                        case Packet.UdpKeepAlive:
                            {
                                #region Reader
                                double time = reader.ReadDouble();
                                #endregion

                                #region Logic
                                PingHandler(owner, time);
                                #endregion
                            }
                            break;
                        case Packet.CustomPacket:
                            {
                                #region Defaults
                                bool isMine;
                                TargetTo targetTo = default(TargetTo);
                                TunnelingTo tunnelingTo = default(TunnelingTo);
                                #endregion

                                #region Reader
                                int viewId = reader.ReadInt();
                                byte packet = reader.ReadPacket();
                                if ((isMine = PlayerHelper.IsMine(owner, viewId)))
                                {
                                    targetTo = (TargetTo)reader.ReadPacket();
                                    tunnelingTo = (TunnelingTo)reader.ReadPacket();
                                }
                                byte[] buffer = reader.ReadWithInteger();
                                #endregion

                                #region Logic
                                CustomPacketHandler(owner, isMine, viewId, buffer, packet, targetTo, tunnelingTo, protocol);
                                #endregion
                            }
                            break;
                        case Packet.AutoSync:
                            {
                                #region Reader
                                RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                                short viewId = reader.ReadShort();
                                byte instanceId = reader.ReadByte();
                                byte[] parameters = reader.ReadNext();
                                #endregion

                                #region Logic
                                OnAutoSyncHandler(neutronPacket, viewId, instanceId, parameters, registerType);
                                #endregion
                            }
                            break;
                        case Packet.Synchronize:
                            {
                                #region Logic
                                SynchronizeHandler(owner, protocol);
                                #endregion
                            }
                            break;
                    }
                }
                else
                    LogHelper.Error("Client is not allowed to run this packet.");
            }
        }
        #endregion

        #region Mono Behaviour
        private void Start()
        {
#if UNITY_EDITOR
            var targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var acl = PlayerSettings.GetApiCompatibilityLevel(targetGroup);
            if (acl == ApiCompatibilityLevel.NET_Standard_2_0)
            {
                //TcpListener.Stop();
                //throw new Exception(".NET Standard is not supported, change to .NET 4.x.");
            }
#endif
#if UNITY_SERVER && !UNITY_EDITOR
            Console.Clear();
#endif
            if (IsReady)
                Initilize();
        }

        private void OnApplicationQuit()
        {
            using (TokenSource)
            {
                if (Initialized)
                {
                    Initialized = false; //* Marca o servidor como off.
                    TokenSource.Cancel(); //* Cancela todos os threads.
                    foreach (NeutronPlayer player in PlayersById.Values)
                        player.Dispose(); //* Libera todos os recursos não gerenciados.
                    _acceptedClients.Dispose();
                    _dataForProcessing.Dispose();
                    TcpListener.Stop();
                }
            }
        }
        #endregion
    }
}