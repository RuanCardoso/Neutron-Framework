using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Constants;
using NeutronNetwork.Server.Internal;
using NeutronNetwork.Helpers;
using System.Net;
using System.Threading.Tasks;
using NeutronNetwork.Extensions;
using System.IO;
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
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : ServerBase
    {
        #region Events
        /// <summary>
        ///* Este evento é acionando quando o servidor é iniciado.
        /// </summary>
        public static NeutronEventNoReturn OnServerStart { get; set; }
        /// <summary>
        ///* Este evento é acionado quando um jogador é conectado ao servidor.
        /// </summary>
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerConnected { get; set; }
        #endregion

        #region Properties
        /// <summary>
        ///* Obtém o status do servidor.
        /// </summary>
        public static bool Initialized { get; set; }
        /// <summary>
        ///* Objeto de jogador que representa o servidor.
        /// </summary>
        public static NeutronPlayer Player { get; set; }
        /// <summary>
        ///* Intância de Neutron do Servidor.
        /// </summary>
        public static Neutron Neutron { get; set; }
        #endregion

        #region Fields -> Collections
        //* Esta fila irá armazenar os clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<TcpClient> _acceptedClients = new NeutronBlockingQueue<TcpClient>(NeutronConstantsSettings.BOUNDED_CAPACITY);
        //* Esta fila irá armazenar os pacotes recebidos dos clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<NeutronPacket> _dataForProcessing = new NeutronBlockingQueue<NeutronPacket>(NeutronConstantsSettings.BOUNDED_CAPACITY);
        //* Esta fila irá fornecer um pool de ID's e atribuirá um ID único para cada cliente novo.
        //* Quando um cliente é desconectado seu ID é reutilizado sendo adicionado a fila novamente.
        public NeutronSafeQueue<int> _pooledIds = new NeutronSafeQueue<int>();
        #endregion

        #region Threading
        //* Este é um token de cancelamento, ele é passado para todos os Threads, é usado para parar os Threads quando o servidor é desligado.
        private readonly CancellationTokenSource TokenSource = new CancellationTokenSource();
        #endregion

        #region Functions
        private void Initilize()
        {
            Neutron = new Neutron();
            Player = new NeutronPlayer()
            {
                IsServer = true,
                Nickname = "Server",
                ID = 0,
            };
            Neutron.IsConnected = true;
            Neutron.Player = Player;

            //* Esta região irá fornecer os ID para a lista.
            #region Provider
            for (int i = 0; i < NeutronModule.Settings.GlobalSettings.MaxPlayers; i++)
                _pooledIds.Enqueue((NeutronConstantsSettings.GENERATE_PLAYER_ID + i) + 1);
            #endregion

            //* Marca o servidor como inicializado.
            Initialized = true;

            #region Logger
            LogHelper.Info("The Server is ready, all protocols(TCP, UDP, RUDP) have been initialized.\r\n");
            #endregion

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
            OnServerStart?.Invoke();
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
                    {
                        _acceptedClients.Add(client, token); //* Nova conexão é enfileirada para processamento.
                    }
                }
                catch (ObjectDisposedException) { continue; }
                catch (OperationCanceledException) { continue; }
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
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
                            tcpClient.NoDelay = OthersHelper.GetSettings().GlobalSettings.NoDelay;
                            tcpClient.ReceiveBufferSize = OthersHelper.GetConstants().ReceiveBufferSize;
                            tcpClient.SendBufferSize = OthersHelper.GetConstants().SendBufferSize;
                            // TODO tcpClient.ReceiveTimeout = int.MaxValue; // only synchronous.
                            // TODO tcpClient.SendTimeout = int.MaxValue; // only synchronous.
                            var player = new NeutronPlayer(ID, tcpClient, new CancellationTokenSource()); //* Cria uma instância do cliente.
                            if (SocketHelper.AddPlayer(player))
                            {
                                Interlocked.Increment(ref PlayerCount); //* Incrementa a quantidade de jogadores do servidor.
                                //* Esta região cria um View, um View é usado para você criar uma comunicação personalizada com o cliente dono(owner).
                                //* Exemplo, dentro do View você pode implementar uma função que envia um evento ou mensagem a cada X Segundos.
                                #region View
                                NeutronSchedule.ScheduleTask(() =>
                                {
                                    GameObject viewGameObject = new GameObject($"View[{player.ID}]");
                                    View viewInstance = null;
                                    if (View != null)
                                        viewInstance = (View)viewGameObject.AddComponent(View.GetType());
                                    else
                                    {
                                        viewInstance = viewGameObject.AddComponent<View>();
                                        if (View == null)
                                            View = gameObject.AddComponent<View>();
                                    }
                                    viewInstance.Player = player;
                                    SceneHelper.MoveToContainer(viewGameObject, "[Container] -> Server");
                                });
                                #endregion

                                LogHelper.Info($"Incoming client, IP: [{((IPEndPoint)player.TcpClient.Client.RemoteEndPoint).Address}] | TCP: [{((IPEndPoint)player.TcpClient.Client.RemoteEndPoint).Port}] | UDP: [{((IPEndPoint)player.UdpClient.Client.LocalEndPoint).Port}] -:[{PlayerCount}]");

                                player.NetworkStream = SocketHelper.GetStream(tcpClient);
                                //* Usa o pool de threads para receber os dados do cliente de forma assíncrona.
                                //* O Pool de threads só deverá ser usado com metódos que não bloqueiam, para evitar gargalos no recebimento de dados.
                                ThreadPool.QueueUserWorkItem((e) =>
                                {
                                    OnReceivingData(player.NetworkStream, player, Protocol.Tcp);
                                    OnReceivingData(player.NetworkStream, player, Protocol.Udp);
                                });
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
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
                    continue;
                }
            }
        }

        //* Inicia o processamento dos pacotes.
        private void PacketProcessingStack()
        {
#if NEUTRON_DEBUG || UNITY_EDITOR
            PacketProcessingStack_ManagedThreadId = ThreadHelper.GetThreadID();
#endif
            CancellationToken token = TokenSource.Token;
            while (!token.IsCancellationRequested)
            {
                try
                {
                    NeutronPacket packet = _dataForProcessing.Take(token); //* Desinfileira os dados e bloqueia o thread se não houver mais dados.
                    RunPacket(packet); //* Processa/executa o pacote.
                }
                catch (ObjectDisposedException) { continue; }
                catch (OperationCanceledException) { continue; }
                catch (Exception ex)
                {
                    LogHelper.StackTrace(ex);
                    continue;
                }
            }
        }

        //* Aqui os dados são enviados aos seus clientes.
        public async void OnSendingData(NeutronPlayer player, NeutronPacket data)
        {
            try
            {
                #region Lag Simulation
                if (NeutronModule.Settings.LagSimulationSettings.Outbound)
                    await Task.Delay(NeutronModule.Settings.LagSimulationSettings.InOutDelay);
                #endregion

                using (NeutronWriter headerWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    byte[] packetBuffer = data.Buffer.Compress();
                    //* Cabeçalho da mensagem/dados.
                    #region Header
                    short playerId = (short)data.Sender.ID;
                    headerWriter.WriteSize(packetBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro(4 bytes), e a mensagem.
                    headerWriter.Write(playerId); //* Pre-fixa o id do jogador no cabeçalho, um short(2 bytes).
                    #endregion
                    byte[] headerBuffer = headerWriter.ToArray();
                    switch (data.Protocol)
                    {
                        case Protocol.Tcp:
                            {
                                NetworkStream networkStream = player.TcpClient.GetStream();
                                await networkStream.WriteAsync(headerBuffer, 0, headerBuffer.Length, player.TokenSource.Token);
#if UNITY_EDITOR
                                //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                NeutronStatistics.m_ServerTCP.AddOutgoing(packetBuffer.Length, data.Packet);
#endif
                            }
                            break;
                        case Protocol.Udp:
                            {
                                if (player.RemoteEndPoint != null) //* Verifica se o IP de destino não é nulo, se sim, o servidor não enviará os dados.
                                {
                                    await player.UdpClient.SendAsync(headerBuffer, headerBuffer.Length, player.RemoteEndPoint);
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ServerUDP.AddOutgoing(packetBuffer.Length, data.Packet);
#endif
                                }
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

        public void OnSimulatingReceivingData(NeutronPacket packet)
        {
            lock (Encapsulate.BeginLock)
            {
                if (Encapsulate.Sender != null)
                    packet = new NeutronPacket(packet.Buffer, packet.Owner, Encapsulate.Sender, packet.Protocol);
                ///////////////////////////////////////////////////////////////////////////////////
                packet.IsServerSide = true;
                ///////////////////////////////////////////////////////////////////////////////////
                _dataForProcessing.Add(packet);
            }
        }

        //* Recebe/Ler os dados dos clientes.
        private async void OnReceivingData(Stream networkStream, NeutronPlayer player, Protocol protocol)
        {
            CancellationToken token = player.TokenSource.Token;
            try
            {
                Packet packet = Packet.Empty;
                byte[] headerBuffer = new byte[NeutronModule.HeaderSize]; //* aqui será armazenado o pre-fixo(tamanho/length) do pacote, que é o tamanho da mensagem transmitida.

                while (!TokenSource.Token.IsCancellationRequested && !token.IsCancellationRequested) // Interrompe o loop em caso de cancelamento do Token, o cancelamento ocorre em desconexões ou exceções.
                {
                    switch (protocol)
                    {
                        case Protocol.Tcp:
                            {
                                if (await SocketHelper.ReadAsyncBytes(networkStream, headerBuffer, 0, NeutronModule.HeaderSize, token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                                {
                                    int size = ByteHelper.ReadSize(headerBuffer); //* converte o buffer em inteiro.
                                    if (size > OthersHelper.GetConstants().MaxTcpPacketSize || size <= 0) //* Verifica se o tamanho da mensagem é válido.
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
#if UNITY_EDITOR
                                            packet = OthersHelper.ReadPacket(packetBuffer);
#endif
                                            _dataForProcessing.Add(new NeutronPacket(packetBuffer, player, player, Protocol.Tcp, packet), token); //* Adiciona os dados na fila para processamento.
                                            {
#if UNITY_EDITOR
                                                //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                                NeutronStatistics.m_ServerTCP.AddIncoming(size, packet);
#endif
                                            }
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
                                var datagram = await player.UdpClient.ReceiveAsync(); //* Recebe os dados enviados pelo cliente.
                                using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    // Monta o cabeçalho dos dados e ler deus dados.
                                    reader.SetBuffer(datagram.Buffer);
                                    ////////////////////////////////////////////////////////////////////////////////////////////
                                    byte[] packetBuffer = reader.ReadSize(out int size); //* ler o pacote.
                                    if (size > OthersHelper.GetConstants().MaxUdpPacketSize || size <= 0)
                                    {
                                        if (!LogHelper.Error($"Invalid udp message size! size: {size}"))
                                            DisconnectHandler(player); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                                    }
                                    else
                                    {
                                        //* descomprime a porra do pacote.
                                        packetBuffer = packetBuffer.Decompress();
                                        //* Esta região funciona como um "Syn/Ack", o cliente envia algum pacote vazio após a conexão, após o servidor receber este pacote, atribui o ip de destino, que é para onde os dados serão enviados.
                                        //! Se o ip de destino é nulo, o servidor não enviará os dados, porque não tem destino, não houve "Syn/Ack".
                                        //! A tentativa de envio sem o "Syn/Ack" causará a exceção de "An existing connection was forcibly closed by the remote host"
                                        #region Syn/Ack
                                        if (player.RemoteEndPoint == null) //* verifica se o ip de destino é nulo, se sim, ele é atribuído com o ip de destino.
                                            player.RemoteEndPoint = datagram.RemoteEndPoint; //* ip de destino do cliente, para onde o servidor irá enviar os dados.
                                        #endregion
#if UNITY_EDITOR
                                        packet = OthersHelper.ReadPacket(packetBuffer);
#endif
                                        _dataForProcessing.Add(new NeutronPacket(packetBuffer, player, player, Protocol.Udp, packet), token); //* Adiciona os dados na fila para processamento.
                                        {
#if UNITY_EDITOR
                                            //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                            NeutronStatistics.m_ServerUDP.AddIncoming(size, packet);
#endif
                                        }
                                    }
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
                LogHelper.StackTrace(ex);
                if (!token.IsCancellationRequested)
                    DisconnectHandler(player);
            }
        }
        #endregion

        #region Packets
        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        void RunPacket(NeutronPacket receivedPacket)
        {
            byte[] packetBuffer = receivedPacket.Buffer;
            bool isServerSide = receivedPacket.IsServerSide;
            Protocol protocol = receivedPacket.Protocol;
            NeutronPlayer owner = receivedPacket.Owner;
            NeutronPlayer sender = receivedPacket.Sender;
            using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
            {
                reader.SetBuffer(packetBuffer);
                switch (reader.ReadPacket<Packet>()) //* Ler o pacote recebido
                {
                    // Test packet
                    case Packet.TcpKeepAlive:
                        {
                            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                            {
                                writer.WritePacket(Packet.TcpKeepAlive);
                                ///////////////////////////////////////////
                                owner.Write(writer, Packet.TcpKeepAlive);
                            }
                        }
                        break;
                    case Packet.Handshake:
                        {
                            #region Reader
                            string appId = reader.ReadString();
                            double time = reader.ReadDouble();
                            #endregion

                            #region Logic
                            if (appId.Decrypt(out appId))
                            {
                                if (OthersHelper.GetSettings().GlobalSettings.AppId == appId)
                                    HandshakeHandler(owner, time);
                                else
                                    owner.Message(Packet.Handshake, "Update your game version, it does not match the current server version.");
                            }
                            else if (!LogHelper.Error("Failed to verify handshak"))
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
                            ChatPacket chatPacket = reader.ReadPacket<ChatPacket>();
                            switch (chatPacket)
                            {
                                case ChatPacket.Global:
                                    tunnelingTo = reader.ReadPacket<TunnelingTo>();
                                    break;
                                case ChatPacket.Private:
                                    viewId = reader.ReadInt32();
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
                            RegisterType registerType = reader.ReadPacket<RegisterType>();
                            TargetTo targetTo = reader.ReadPacket<TargetTo>();
                            Cache cache = reader.ReadPacket<Cache>();
                            short viewId = reader.ReadInt16();
                            byte rpcId = reader.ReadByte();
                            byte instanceId = reader.ReadByte();
                            byte[] buffer = reader.ReadNextBytes(packetBuffer.Length);
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
                            byte[] buffer = reader.ReadNextBytes(packetBuffer.Length);
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
                            int channelId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            JoinChannelHandler(owner, channelId);
                            #endregion
                        }
                        break;
                    case Packet.GetChached:
                        {
                            #region Reader
                            CachedPacket cachedPacket = reader.ReadPacket<CachedPacket>();
                            int packetId = reader.ReadInt32();
                            bool includeMe = reader.ReadBoolean();
                            #endregion

                            #region Logic
                            GetCacheHandler(owner, cachedPacket, packetId, includeMe);
                            #endregion
                        }
                        break;
                    case Packet.CreateRoom:
                        {
                            #region Reader
                            string password = reader.ReadString();
                            NeutronRoom room = reader.ReadIntExactly<NeutronRoom>();
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
                            int roomId = reader.ReadInt32();
                            #endregion

                            #region Logic
                            JoinRoomHandler(owner, roomId);
                            #endregion
                        }
                        break;
                    case Packet.Leave:
                        {
                            #region Reader
                            MatchmakingPacket packet = reader.ReadPacket<MatchmakingPacket>();
                            #endregion

                            #region Logic
                            if (packet == MatchmakingPacket.Room)
                                LeaveRoomHandler(owner);
                            else if (packet == MatchmakingPacket.Channel)
                                LeaveChannelHandler(owner);
                            #endregion
                        }
                        break;
                    case Packet.DestroyPlayer:
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
                    case Packet.Ping:
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
                            bool isMine = false;
                            TargetTo targetTo = default(TargetTo);
                            TunnelingTo tunnelingTo = default(TunnelingTo);
                            #endregion

                            #region Reader
                            int viewId = reader.ReadInt32();
                            CustomPacket packet = reader.ReadPacket<CustomPacket>();
                            if ((isMine = PlayerHelper.IsMine(owner, viewId)))
                            {
                                targetTo = reader.ReadPacket<TargetTo>();
                                tunnelingTo = reader.ReadPacket<TunnelingTo>();
                            }
                            byte[] buffer = reader.ReadIntExactly();
                            #endregion

                            #region Logic
                            CustomPacketHandler(owner, isMine, viewId, buffer, packet, targetTo, tunnelingTo, protocol);
                            #endregion
                        }
                        break;
                    case Packet.OnAutoSync:
                        {
                            #region Reader
                            RegisterType registerType = reader.ReadPacket<RegisterType>();
                            short viewId = reader.ReadInt16();
                            byte instanceId = reader.ReadByte();
                            byte[] buffer = reader.ReadNextBytes(packetBuffer.Length);
                            #endregion

                            #region Logic
                            OnAutoSyncHandler(owner, viewId, instanceId, buffer, registerType, protocol, isServerSide);
                            #endregion
                        }
                        break;
                }
            }
        }
        #endregion

        #region MonoBehaviour
        private void Start()
        {
#if UNITY_SERVER
            Console.Clear();
#endif

#if UNITY_EDITOR
            var targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var acl = PlayerSettings.GetApiCompatibilityLevel(targetGroup);
            if (acl != ApiCompatibilityLevel.NET_Standard_2_0)
            {
                if (IsReady)
                    Initilize();
            }
            else
                LogHelper.Error(".NET Standard is not supported, change to .NET 4.x.");
#else
            if (IsReady)
                Initilize();
#endif
        }

        private async void OnApplicationQuit()
        {
            using (TokenSource)
            {
                if (Initialized)
                {
                    Initialized = false;
                    TokenSource.Cancel();
                    await Task.Delay(50);
                    foreach (var player in PlayersById.Values)
                    {
                        player.TokenSource.Cancel();
                        await Task.Delay(20);
                        player.Dispose();
                    }
                    _acceptedClients.Dispose();
                    _dataForProcessing.Dispose();
                    TcpListener.Stop();
                }
            }
        }
        #endregion
    }
}