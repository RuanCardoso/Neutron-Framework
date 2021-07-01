using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Constants;
using NeutronNetwork.Server.Internal;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Wrappers;
using System.Net;
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
    [RequireComponent(typeof(NeutronConfig))]
    [RequireComponent(typeof(NeutronDispatcher))]
    [RequireComponent(typeof(NeutronEvents))]
    [RequireComponent(typeof(NeutronStatistics))]
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : NeutronServerPublicFunctions
    {
        #region Events
        /// <summary>
        ///* Este evento é acionando quando o servidor é iniciado.
        /// </summary>
        public static NeutronEventNoReturn OnServerStart = new NeutronEventNoReturn();
        /// <summary>
        ///* Este evento é acionado quando um jogador é conectado ao servidor.
        /// </summary>
        public static NeutronEventNoReturn<Player> OnPlayerConnected = new NeutronEventNoReturn<Player>();
        #endregion

        #region Variables
        /// <summary>
        ///* Obtém o status do servidor.
        /// </summary>
        public static bool Initialized;
        #endregion

        #region Collections
        //* Esta fila irá armazenar os clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private NeutronBlockingQueue<TcpClient> acceptedClients = new NeutronBlockingQueue<TcpClient>();
        //* Esta fila irá armazenar os pacotes recebidos dos clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private NeutronBlockingQueue<DataBuffer> dataForProcessing = new NeutronBlockingQueue<DataBuffer>();
        //* Esta fila irá fornecer um pool de ID's e atribuirá um ID único para cada cliente novo.
        //* Quando um cliente for desconectado seu ID será reutilizado sendo adicionado a fila novamente.
        public NeutronSafeQueue<int> generatedIds = new NeutronSafeQueue<int>();
        #endregion

        #region Threading
        //* Este é um token de cancelamento, ele é passado para todos os Threads criados, é usado para parar os Threads quando o servidor for desligado.
        private CancellationTokenSource _cts = new CancellationTokenSource();
        #endregion

        #region Functions
        private void InitilizeServer()
        {
            //* Esta região irá fornecer os ID para a lista.
            #region Provider
            for (int i = 0; i < NeutronConfig.Settings.GlobalSettings.MaxPlayers; i++)
                generatedIds.Enqueue((NeutronConstants.GENERATE_PLAYER_ID + i) + 1);
            #endregion

            //* Marca o servidor como inicializado.
            Initialized = true;

            #region Logger
            NeutronLogger.Logger("The Server is ready, all protocols have been initialized.\r\n");
            #endregion

            //* Este thread será dedicado a aceitar e enfileirar os novos clientes.
            #region Threads
            Thread acptTh = new Thread((t) => OnAcceptedClient())
            {
                Priority = System.Threading.ThreadPriority.Normal,
                IsBackground = true,
                Name = "Neutron acptTh"
            };
            acptTh.Start();

            //* Este thread será dedicado a desinfileirar os pacotes e processa-los.
            Thread dataForProcessingTh = new Thread((e) => ServerDataProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Highest,
                IsBackground = true,
                Name = "Neutron dataForProcessingTh"
            };
            dataForProcessingTh.Start();

            //* Este thread será dedicado a desinfileirar os novos clientes e processa-los.
            Thread stackProcessingAcceptedConnectionsTh = new Thread((e) => AcceptedConnectionsProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Normal,
                IsBackground = true,
                Name = "Neutron stackProcessingAcceptedConnectionsTh"
            };
            stackProcessingAcceptedConnectionsTh.Start();
            #endregion

            #region Events
            OnServerStart.Invoke();
            #endregion
        }

        //* Aceita os clientes e os adiciona a fila.
        private async void OnAcceptedClient()
        {
            try
            {
                CancellationToken token = _cts.Token;
                while (Initialized && !token.IsCancellationRequested)
                {
                    TcpClient tcpClient = await TcpSocket.AcceptTcpClientAsync(); //* Aceita a nova conexão,
                    {
                        acceptedClients.Add(tcpClient, token); //* Nova conexão é enfileirada para processamento.
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); }
        }

        //* Inicia o processamento dos pacotes.
        private void ServerDataProcessingStack()
        {
            try
            {
                CancellationToken token = _cts.Token;
                while (Initialized && !token.IsCancellationRequested)
                {
                    //! [OBSOLETE]: dataForProcessing.mEvent.Reset(); //* Permite fazer o bloqueio do Thread novamente.
                    //! [OBSOLETE]: while (dataForProcessing.SafeCount > 0)
                    {
                        for (int i = 0; i < NeutronConfig.Settings.ServerSettings.PacketChunkSize; i++)
                        {
                            DataBuffer data = dataForProcessing.Take(token); //* Desinfileira os dados e bloqueia o thread se não houver mais dados.
                            {
                                PacketProcessing(data.player, data.buffer, data.protocol); //* Processa o pacote.
                            }
                        }
                    }
                    //! [OBSOLETE]: dataForProcessing.mEvent.WaitOne(); //* Bloqueia o Thread após processar todos os dados.
                }
            }
            catch (OperationCanceledException) { }
            catch (ArgumentNullException) { }
        }

        //* Inicia o processamento dos clientes.
        private void AcceptedConnectionsProcessingStack()
        {
            try
            {
                CancellationToken token = _cts.Token;
                while (Initialized && !token.IsCancellationRequested)
                {
                    //! [OBSOLETE]: acceptedClients.mEvent.Reset(); //* Permite fazer o bloqueio do Thread novamente.
                    //! [OBSOLETE]: while (acceptedClients.SafeCount > 0)
                    {
                        TcpClient acceptedClient = acceptedClients.Take(token); //* Desinfileira os clientes e bloqueia o thread se não houver mais dados.
                        if (PlayerHelper.GetAvailableID(out int ID))
                        {
                            if (SocketHelper.LimitConnectionsByIP(acceptedClient)) //* Verifica se este cliente antigiu o limite de conexões ativas.
                            {
                                acceptedClient.NoDelay = NeutronConfig.Settings.GlobalSettings.NoDelay;
                                // TODO acceptedClient.ReceiveTimeout = int.MaxValue;
                                // TODO acceptedClient.SendTimeout = int.MaxValue;
                                var cancellationTokenSource = new CancellationTokenSource(); //* Um token para cancelar todas as operações em execução do cliente, após ele ser desconectado.
                                var nPlayer = new Player(ID, acceptedClient, cancellationTokenSource); //* Cria uma instância do cliente.
                                if (SocketHelper.AddPlayer(nPlayer))
                                {
                                    //* Esta região cria um View, um View é usado para você criar uma comunicação personalizada com o cliente dono(owner).
                                    //* Exemplo, dentro do View você pode implementar uma função que envia um evento ou mensagem a cada X Segundos.
                                    #region View
                                    NeutronDispatcher.Dispatch(() =>
                                    {
                                        GameObject iView = new GameObject($"View[{nPlayer.ID}]");
                                        View View = iView.AddComponent<View>();
                                        View.Owner = nPlayer;
                                        SceneHelper.MoveToContainer(iView, "[Container] -> Server");
                                    });
                                    OnPlayerConnected.Invoke(nPlayer);
                                    #endregion
                                    Interlocked.Increment(ref CurrentPlayers);

                                    #region Logger
                                    NeutronLogger.Logger($"Incoming client, IP: [{nPlayer.RemoteEndPoint().Address}] | TCP: [{nPlayer.RemoteEndPoint().Port}] | UDP: [{((IPEndPoint)nPlayer.udpClient.Client.LocalEndPoint).Port}] -:[0]");
                                    #endregion

                                    //* Este thread será dedicado a desinfileirar e enviar os dados do seu cliente.
                                    //! Obs: Um Thread novo pra cada cliente é criado, poderiamos usar o ThreadPool? 
                                    //* Não, este metódo bloqueia o thread, não queremos bloquear os threads do pool de threads, né?
                                    //* Porque o pool de threads está sendo usado para receber os dados de forma assíncrona, bloquea-los com este metódo causará gargalos em threads que estão recebendo dados.
                                    Thread procTh = new Thread(() => OnProcessData(nPlayer, cancellationTokenSource.Token))
                                    {
                                        IsBackground = true,
                                        Name = $"procTh[{ID}]",
                                    };
                                    procTh.Start();

                                    //* Usa o pool de threads para receber os dados do cliente de forma assíncrona.
                                    //* O Pool de threads só deverá ser usado com metódos que não bloqueiam, para evitar gargalos no recebimento de dados.
                                    ThreadPool.QueueUserWorkItem((e) =>
                                    {
                                        OnReceiveData(nPlayer, Protocol.Tcp, e);
                                        OnReceiveData(nPlayer, Protocol.Udp, e);
                                    }, cancellationTokenSource.Token);
                                }
                                else
                                {
                                    #region Logger
                                    NeutronLogger.LoggerError("Failed to add Player!");
                                    #endregion
                                    acceptedClient.Close();
                                    continue;
                                }
                            }
                            else
                            {
                                #region Logger
                                NeutronLogger.LoggerError("Client not allowed!");
                                #endregion
                                acceptedClient.Close();
                                continue;
                            }
                        }
                        else
                        {
                            #region Logger
                            NeutronLogger.LoggerError("Max Players Reached");
                            #endregion
                            acceptedClient.Close();
                            continue;
                        }
                    }
                    //! [OBSOLETE]: acceptedClients.mEvent.WaitOne(); //* Bloqueia o Thread após processar todos os dados.
                }
            }
            catch (OperationCanceledException) { }
            catch (ArgumentNullException) { }
        }

        //* Aqui os dados são enviados aos seus clientes.
        private void OnProcessData(Player nPlayer, object toToken)
        {
            try
            {
                CancellationToken token = (CancellationToken)toToken;
                NeutronBlockingQueue<DataBuffer> queueData = nPlayer.qData;
                NetworkStream netStream = nPlayer.tcpClient.GetStream();

                while (Initialized && !token.IsCancellationRequested)
                {
                    //! [OBSOLETE]: queueData.mEvent.Reset(); //* Permite fazer o bloqueio do Thread novamente.
                    //! [OBSOLETE]: while (queueData.SafeCount > 0)
                    {
                        for (int i = 0; i < NeutronConfig.Settings.ServerSettings.ProcessChunkSize; i++)
                        {
                            DataBuffer data = queueData.Take(token); //* Desinfileira os dados e bloqueia o thread se não houver mais dados.
                            {
                                using (NeutronWriter header = Neutron.PooledNetworkWriters.Pull())
                                {
                                    int dataLength = data.buffer.Length;

                                    //* Cabeçalho da mensagem/dados.
                                    #region Header
                                    header.SetLength(0);
                                    header.WriteFixedLength(dataLength); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro(4bytes).
                                    header.Write(data.buffer); //* Os dados que serão enviados junto com o pre-fixo.
                                    byte[] nBuffer = header.ToArray();
                                    #endregion

                                    //* Envia os dados, o envio de dados bloqueia o thread, este é uo motivo de não ser usado o ThreadPool.
                                    //! A Função assíncrona de envio trava a API devido ao bloqueio de Thread do ManualResetEvent.
                                    switch (data.protocol)
                                    {
                                        case Protocol.Tcp:
                                            {
                                                netStream.Write(nBuffer, 0, nBuffer.Length);
                                                {
                                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                                    NeutronStatistics.m_ServerTCP.AddOutgoing(dataLength);
                                                }
                                            }
                                            break;
                                        case Protocol.Udp:
                                            {
                                                if (nPlayer.rPEndPoint != null) //* Verifica se o IP de destino não é nulo, se o ip de destino for nulo, o servidor não enviará os dados.
                                                {
                                                    nPlayer.udpClient.Send(data.buffer, dataLength, nPlayer.rPEndPoint);
                                                    {
                                                        //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                                        NeutronStatistics.m_ServerUDP.AddOutgoing(dataLength);
                                                    }
                                                }
                                            }
                                            break;
                                    }
                                }
                            }
                        }
                    }
                    //! [OBSOLETE]: queueData.mEvent.WaitOne(); //* Bloqueia o Thread após processar todos os dados.
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (OperationCanceledException) { }
            catch (ArgumentNullException) { }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); }
        }

        //* Recebe/Ler os dados dos clientes.
        private async void OnReceiveData(Player nPlayer, Protocol protocol, object toToken)
        {
            try
            {
                byte[] header = new byte[sizeof(int)]; //* aqui será armazenado o pre-fixo do cabeçalho, que é o tamanho da mensagem enviada pelo cliente.

                CancellationToken token = (CancellationToken)toToken;
                NetworkStream netStream = nPlayer.tcpClient.GetStream();

                while (Initialized && !token.IsCancellationRequested)
                {
                    if (protocol == Protocol.Tcp)
                    {
                        if (await SocketHelper.ReadAsyncBytes(netStream, header, 0, sizeof(int), token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                        {
                            int size = BitConverter.ToInt32(header, 0); //* converte o buffer do pre-fixo de volta em inteiro.
                            if (size > MAX_RECEIVE_MESSAGE_SIZE || size <= 0) //* Verifica se o tamanho da mensagem é válido.
                                DisconnectHandler(nPlayer);
                            else
                            {
                                byte[] message = new byte[size]; //* cria um buffer com o tamanho da mensagem/pre-fixo.
                                if (await SocketHelper.ReadAsyncBytes(netStream, message, 0, size, token)) //* ler a mensagem e armazena no buffer de mensagem.
                                {
                                    dataForProcessing.Add(new DataBuffer(message, nPlayer, Protocol.Tcp), token); //* Adiciona os dados na fila para processamento.
                                    {
                                        //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                        NeutronStatistics.m_ServerTCP.AddIncoming(size);
                                    }
                                }
                                else DisconnectHandler(nPlayer); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                            }
                        }
                        else DisconnectHandler(nPlayer);
                    }
                    else if (protocol == Protocol.Udp)
                    {
                        if (!token.IsCancellationRequested)
                        {
                            var udpReceiveResult = await nPlayer.udpClient.ReceiveAsync(); //* Recebe os dados enviados pelo cliente.
                            if (udpReceiveResult.Buffer.Length > 0)
                            {
                                //* Esta região funciona como um "Handshake", o cliente envia algum pacote vazio após a conexão, após o servidor receber este pacote, atribui o ip de destino, que é para onde os dados serão enviados.
                                //! Se o ip de destino for nulo, o servidor não enviará os dados, porque não tem destino, não houve "Handshake".
                                //! A tentativa de envio sem o "Handshake" causará a exceção de "An existing connection was forcibly closed by the remote host"
                                #region Handshake
                                if (nPlayer.rPEndPoint == null) //* verifica se o ip de destino é nulo, se for, ele é atribuído com o ip de destino.
                                    nPlayer.rPEndPoint = udpReceiveResult.RemoteEndPoint; //* ip de destino do cliente, para onde o servidor irá enviar os dados.
                                #endregion

                                dataForProcessing.Add(new DataBuffer(udpReceiveResult.Buffer, nPlayer, Protocol.Udp), token); //* Adiciona os dados na fila para processamento.
                                {
                                    //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                    NeutronStatistics.m_ServerUDP.AddIncoming(udpReceiveResult.Buffer.Length);
                                }
                            }
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (ThreadAbortException) { }
            catch (SocketException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                if (!NeutronLogger.LoggerError("OnReceiveData exception!"))
                    NeutronLogger.StackTrace(ex);
            }
        }
        #endregion

        #region Packets
        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        void PacketProcessing(Player nSender, byte[] buffer, Protocol protocol)
        {
#if UNITY_SERVER || UNITY_EDITOR
            try
            {
                using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
                {
                    nReader.SetBuffer(buffer);
                    switch (nReader.ReadPacket<SystemPacket>()) //* Ler o pacote recebido
                    {
                        case SystemPacket.Handshake:
                            {
                                #region Logic
                                HandshakeHandler(nSender);
                                #endregion
                            }
                            break;
                        case SystemPacket.Nickname:
                            {
                                #region Reader
                                string nickname = nReader.ReadString();
                                #endregion

                                #region Logic
                                NicknameHandler(nSender, nickname);
                                #endregion
                            }
                            break;
                        case SystemPacket.Chat:
                            {
                                #region Defaults
                                Broadcast broadcast = default(Broadcast);
                                int networkID = default(int);
                                #endregion

                                #region Reader
                                ChatPacket chatPacket = nReader.ReadPacket<ChatPacket>();
                                switch (chatPacket)
                                {
                                    case ChatPacket.Global:
                                        broadcast = nReader.ReadPacket<Broadcast>();
                                        break;
                                    case ChatPacket.Private:
                                        networkID = nReader.ReadInt32();
                                        break;
                                }
                                string message = nReader.ReadString();
                                #endregion

                                #region Logic
                                ChatHandler(nSender, chatPacket, broadcast, networkID, message);
                                #endregion
                            }
                            break;
                        case SystemPacket.iRPC:
                            {
                                #region Reader
                                Broadcast broadcast = nReader.ReadPacket<Broadcast>();
                                SendTo sendTo = nReader.ReadPacket<SendTo>();
                                CacheMode cacheMode = nReader.ReadPacket<CacheMode>();
                                int networkID = nReader.ReadInt32();
                                int attributeID = nReader.ReadInt32();
                                byte[] parameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                DynamicHandler(nSender, broadcast, sendTo, cacheMode, networkID, attributeID, parameters, protocol);
                                #endregion
                            }
                            break;
                        case SystemPacket.sRPC:
                            {
                                #region Reader
                                int networkID = nReader.ReadInt32();
                                int attributeID = nReader.ReadInt32();
                                byte[] parameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                sRPCHandler(nSender, networkID, attributeID, parameters);
                                #endregion
                            }
                            break;
                        case SystemPacket.GetChannels:
                            {
                                #region Logic
                                GetChannelsHandler(nSender);
                                #endregion
                            }
                            break;
                        case SystemPacket.JoinChannel:
                            {
                                #region Reader
                                int channelID = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                JoinChannelHandler(nSender, channelID);
                                #endregion
                            }
                            break;
                        case SystemPacket.GetChached:
                            {
                                #region Reader
                                CachedPacket cachedPacket = nReader.ReadPacket<CachedPacket>();
                                int packetID = nReader.ReadInt32();
                                bool includeMe = nReader.ReadBoolean();
                                #endregion

                                #region Logic
                                GetCacheHandler(nSender, cachedPacket, packetID, includeMe);
                                #endregion
                            }
                            break;
                        case SystemPacket.CreateRoom:
                            {
                                #region Reader
                                string roomName = nReader.ReadString();
                                int maxPlayers = nReader.ReadInt32();
                                string password = nReader.ReadString();
                                bool isVisible = nReader.ReadBoolean();
                                bool joinOrCreate = nReader.ReadBoolean();
                                string options = nReader.ReadString();
                                #endregion

                                #region Logic
                                CreateRoomHandler(nSender, roomName, maxPlayers, password, isVisible, joinOrCreate, options);
                                #endregion
                            }
                            break;
                        case SystemPacket.GetRooms:
                            {
                                #region Logic
                                GetRoomsHandler(nSender);
                                #endregion
                            }
                            break;
                        case SystemPacket.JoinRoom:
                            {
                                #region Reader
                                int roomID = nReader.ReadInt32();
                                #endregion

                                #region Logic
                                JoinRoomHandler(nSender, roomID);
                                #endregion
                            }
                            break;
                        case SystemPacket.Leave:
                            {
                                #region Reader
                                MatchmakingPacket subPacket = nReader.ReadPacket<MatchmakingPacket>();
                                #endregion

                                #region Logic
                                if (subPacket == MatchmakingPacket.Room)
                                    LeaveRoomHandler(nSender);
                                else if (subPacket == MatchmakingPacket.Channel)
                                    LeaveChannelHandler(nSender);
                                #endregion
                            }
                            break;
                        case SystemPacket.DestroyPlayer:
                            {
                                #region Logic
                                DestroyPlayerHandler(nSender);
                                #endregion
                            }
                            break;
                        case SystemPacket.SetPlayerProperties:
                            {
                                #region Reader
                                string properties = nReader.ReadString();
                                #endregion

                                #region Logic
                                SetPlayerPropertiesHandler(nSender, properties);
                                #endregion
                            }
                            break;
                        case SystemPacket.SetRoomProperties:
                            {
                                #region Reader
                                string properties = nReader.ReadString();
                                #endregion

                                #region Logic
                                SetRoomPropertiesHandler(nSender, properties);
                                #endregion
                            }
                            break;
                        case SystemPacket.Heartbeat:
                            {
                                #region Reader
                                double time = nReader.ReadDouble();
                                #endregion

                                #region Logic
                                HeartbeatHandler(nSender, time);
                                #endregion
                            }
                            break;
                        case SystemPacket.ClientPacket:
                            {
                                #region Defaults
                                bool isMine = false;
                                SendTo sendTo = default(SendTo);
                                Broadcast broadcast = default(Broadcast);
                                #endregion

                                #region Reader
                                int networkID = nReader.ReadInt32();
                                ClientPacket clientPacket = nReader.ReadPacket<ClientPacket>();
                                if ((isMine = PlayerHelper.IsMine(nSender, networkID)))
                                {
                                    sendTo = nReader.ReadPacket<SendTo>();
                                    broadcast = nReader.ReadPacket<Broadcast>();
                                }
                                Protocol recProtocol = nReader.ReadPacket<Protocol>();
                                byte[] parameters = nReader.ReadExactly();
                                #endregion

                                #region Logic
                                ClientPacketHandler(nSender, isMine, networkID, clientPacket, sendTo, broadcast, recProtocol, parameters);
                                #endregion
                            }
                            break;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); }
#endif
        }
        #endregion

        #region MonoBehaviour
        private void Start()
        {
#if UNITY_SERVER
            Console.Clear();
#endif
#if UNITY_EDITOR
            BuildTargetGroup targetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            ApiCompatibilityLevel Acl = PlayerSettings.GetApiCompatibilityLevel(targetGroup);
            if (Acl != ApiCompatibilityLevel.NET_Standard_2_0)
                Init();
            else NeutronLogger.LoggerError(".NET Standard is not supported, change to .NET 4.x.");
#else
#if !NET_STANDARD_2_0
            Init();
#else
NeutronLogger.LoggerError(".NET Standard is not supported, change to .NET 4.x.");
#endif
#endif
            void Init()
            {
                if (!IsReady)
                    NeutronLogger.LoggerError("The server could not be initialized ):");
                else InitilizeServer();
            }
        }

        private void OnApplicationQuit()
        {
            using (_cts)
            {
                //* Desliga o servidor.
                Initialized = false;
                #region Dispose
                SocketHelper.Dispose();
                #endregion

                #region Token
                _cts.Cancel();
                #endregion

                #region Dispose
                acceptedClients.Dispose();
                dataForProcessing.Dispose();
                #endregion
            }
        }
        #endregion
    }
}