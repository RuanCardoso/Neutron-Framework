using System;
using System.Net.Sockets;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using System.Net;
using System.Threading.Tasks;
using NeutronNetwork.Extensions;
using System.IO;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Wrappers;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Internal;
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
        public static NeutronEventNoReturn OnStart { get; set; }
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
        public NeutronPlayer Player { get; set; }
        /// <summary>
        ///* Intância de Neutron do Servidor.
        /// </summary>
        public Neutron Neutron { get; set; }
        #endregion

        #region Fields -> Collections
        //* Esta fila irá armazenar os clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<TcpClient> _acceptedClients = new NeutronBlockingQueue<TcpClient>(NeutronConstantsSettings.BOUNDED_CAPACITY);
        //* Esta fila irá armazenar os pacotes recebidos dos clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private readonly NeutronBlockingQueue<NeutronPacket> _dataForProcessing = new NeutronBlockingQueue<NeutronPacket>(NeutronConstantsSettings.BOUNDED_CAPACITY);
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
            Player = new NeutronPlayer()
            {
                IsServer = true,
                Nickname = "Server",
                ID = 0,
            };

            Neutron = new Neutron
            {
                IsConnected = true,
                IsServer = true,
                Player = Player
            };

#if UNITY_SERVER || UNITY_NEUTRON_LAN
            Neutron.Client = Neutron;
#endif

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
                            tcpClient.ReceiveBufferSize = OthersHelper.GetConstants().TcpReceiveBufferSize;
                            tcpClient.SendBufferSize = OthersHelper.GetConstants().TcpSendBufferSize;
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

                                //* Usa o pool de threads para receber os dados do cliente de forma assíncrona.
                                //* O Pool de threads só deverá ser usado com metódos que não bloqueiam, para evitar gargalos no recebimento de dados.
                                //* Utilizando o pool de Threads, em vez de criar um thread novo para cada conexão.
                                switch (OthersHelper.GetConstants().ReceiveThread)
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
                    switch (OthersHelper.GetConstants().PacketThread)
                    {
                        case ThreadType.Neutron:
                            RunPacket(packet); //* Processa/executa o pacote.
                            break;
                        case ThreadType.Unity:
                            {
                                NeutronSchedule.ScheduleTask(() =>
                                {
                                    RunPacket(packet); //* Processa/executa o pacote.
                                });
                            }
                            break;
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

        //* Aqui os dados são enviados aos seus clientes.
        public void OnSendingData(NeutronPlayer player, NeutronPacket data)
        {
            try
            {
                using (NeutronWriter header = Neutron.PooledNetworkWriters.Pull())
                {
                    byte[] packetBuffer = data.Buffer.Compress();
                    //* Cabeçalho da mensagem/dados.
                    #region Header
                    short playerId = (short)data.Sender.ID;
                    header.WriteSize(packetBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho e a mensagem.
                    header.Write(playerId); //* Pre-fixa o id do jogador no cabeçalho, um short(2 bytes).
                    #endregion
                    byte[] headerBuffer = header.ToArray();
                    switch (data.Protocol)
                    {
                        case Protocol.Tcp:
                            {
                                NetworkStream networkStream = player.TcpClient.GetStream();
                                switch (OthersHelper.GetConstants().SendModel)
                                {
                                    case SendType.Synchronous:
                                        networkStream.Write(headerBuffer, 0, headerBuffer.Length);
                                        break;
                                    default:
                                        if (OthersHelper.GetConstants().SendAsyncPattern == AsynchronousType.APM)
                                            networkStream.Write(headerBuffer, 0, headerBuffer.Length);
                                        else
                                            SocketHelper.SendTcpAsync(networkStream, headerBuffer, player.TokenSource.Token);
                                        break;
                                }
#if UNITY_EDITOR
                                //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                NeutronStatistics.ServerTCP.AddOutgoing(packetBuffer.Length, data.Packet);
#endif
                            }
                            break;
                        case Protocol.Udp:
                            {
                                player.StateObject.SendDatagram = headerBuffer;
                                if (player.StateObject.UdpIsReady()) //* Verifica se o IP de destino não é nulo, se sim, o servidor não enviará os dados.
                                {
                                    NonAllocEndPoint remoteEp = (NonAllocEndPoint)player.StateObject.NonAllocEndPoint;
                                    switch (OthersHelper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            SocketHelper.SendBytes(player.UdpClient, headerBuffer, remoteEp);
                                            break;
                                        default:
                                            {
                                                switch (OthersHelper.GetConstants().SendAsyncPattern)
                                                {
                                                    case AsynchronousType.APM:
                                                        {
                                                            SocketHelper.BeginSendBytes(player.UdpClient, headerBuffer, remoteEp, (ar) =>
                                                            {
                                                                SocketHelper.EndSendBytes(player.UdpClient, ar);
                                                            }); //* Envia os dados pro servidor.
                                                            break;
                                                        }

                                                    default:
                                                        SocketHelper.SendUdpAsync(player.UdpClient, player.StateObject, remoteEp);
                                                        break;
                                                }
                                                break;
                                            }
                                    }
#if UNITY_EDITOR
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ServerUDP.AddOutgoing(packetBuffer.Length, data.Packet);
#endif
                                }
                                else
                                    LogHelper.Error("Udp Endpoint is null!");
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
                //***********************************************************
                packet.IsServerSide = true;
                //***********************************************************
                _dataForProcessing.Add(packet);
            }
        }

        private void CreateUdpPacket(NeutronPlayer player, Packet packet)
        {
            byte[] datagram = player.StateObject.ReceivedDatagram;
            //* descomprime a porra do pacote.
            byte[] packetBuffer = datagram.Decompress();
#if UNITY_EDITOR
            packet = (Packet)packetBuffer[0];
#endif
            NeutronPacket serverPacket = Neutron.PooledNetworkPackets.Pull();
            serverPacket.Buffer = packetBuffer;
            serverPacket.Owner = player;
            serverPacket.Sender = player;
            serverPacket.Protocol = Protocol.Udp;
            serverPacket.Packet = packet;
            _dataForProcessing.Add(serverPacket, player.TokenSource.Token); //* Adiciona os dados na fila para processamento.
            {
#if UNITY_EDITOR
                //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                NeutronStatistics.ServerUDP.AddIncoming(datagram.Length, packet);
#endif
            }
        }

        private void UdpApmReceive(NeutronPlayer player, Packet packet)
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
                    //********************************************************************************************
                    Buffer.BlockCopy(player.StateObject.Buffer, 0, player.StateObject.ReceivedDatagram, 0, bytesRead);
                    //********************************************************************************************
                    CreateUdpPacket(player, packet);
                }
                UdpApmReceive(player, packet);
            });
        }

        //* Recebe/Ler os dados dos clientes.
        private async void OnReceivingData(Stream networkStream, NeutronPlayer player, Protocol protocol)
        {
            CancellationToken token = player.TokenSource.Token;
            try
            {
                Packet packet = Packet.Empty;
                byte[] headerBuffer = new byte[NeutronModule.HeaderSize]; //* aqui será armazenado o pre-fixo(tamanho/length) do pacote, que é o tamanho da mensagem transmitida.
                bool apm = false;
                while ((!TokenSource.Token.IsCancellationRequested && !token.IsCancellationRequested) && !apm) // Interrompe o loop em caso de cancelamento do Token, o cancelamento ocorre em desconexões ou exceções.
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
                                            packet = (Packet)packetBuffer[0];
#endif
                                            NeutronPacket serverPacket = Neutron.PooledNetworkPackets.Pull();
                                            serverPacket.Buffer = packetBuffer;
                                            serverPacket.Owner = player;
                                            serverPacket.Sender = player;
                                            serverPacket.Protocol = Protocol.Tcp;
                                            serverPacket.Packet = packet;
                                            _dataForProcessing.Add(serverPacket, token); //* Adiciona os dados na fila para processamento.
                                            {
#if UNITY_EDITOR
                                                //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                                NeutronStatistics.ServerTCP.AddIncoming(size, packet);
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
                                if (OthersHelper.GetConstants().ReceiveAsyncPattern == AsynchronousType.TAP)
                                {
                                    if (await SocketHelper.ReadAsyncBytes(player.UdpClient, player.StateObject))
                                        CreateUdpPacket(player, packet);
                                }
                                else
                                {
                                    UdpApmReceive(player, packet);
                                    apm = true;
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
        private void RunPacket(NeutronPacket receivedPacket)
        {
            byte[] packetBuffer = receivedPacket.Buffer;
            bool isServerSide = receivedPacket.IsServerSide;
            Protocol protocol = receivedPacket.Protocol;
            NeutronPlayer owner = receivedPacket.Owner;
            NeutronPlayer sender = receivedPacket.Sender;
            using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
            {
                reader.SetBuffer(packetBuffer);
                //************************************************
                Packet outPacket = (Packet)reader.ReadPacket();
                //************************************************
                if (OnReceivePacket(outPacket) || isServerSide)
                {
                    switch (outPacket) //* Ler o pacote recebido
                    {
                        // Test packet
                        case Packet.TcpKeepAlive:
                            {
                                using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
                                {
                                    writer.WritePacket((byte)Packet.TcpKeepAlive);
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
                                Authentication auth = reader.ReadIntExactly<Authentication>();
                                #endregion

                                #region Logic
                                if (appId.Decrypt(out appId))
                                {
                                    if (OthersHelper.GetSettings().GlobalSettings.AppId == appId)
                                        HandshakeHandler(owner, time, auth);
                                    else
                                        owner.Message(Packet.Handshake, "Update your game version, it does not match the current server version.");
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
                                RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                                TargetTo targetTo = (TargetTo)reader.ReadPacket();
                                CacheMode cache = (CacheMode)reader.ReadPacket();
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
                                CachedPacket cachedPacket = (CachedPacket)reader.ReadPacket();
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
                                bool isMine;
                                TargetTo targetTo = default(TargetTo);
                                TunnelingTo tunnelingTo = default(TunnelingTo);
                                #endregion

                                #region Reader
                                int viewId = reader.ReadInt32();
                                CustomPacket packet = (CustomPacket)reader.ReadPacket();
                                if ((isMine = PlayerHelper.IsMine(owner, viewId)))
                                {
                                    targetTo = (TargetTo)reader.ReadPacket();
                                    tunnelingTo = (TunnelingTo)reader.ReadPacket();
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
                                RegisterMode registerType = (RegisterMode)reader.ReadPacket();
                                short viewId = reader.ReadInt16();
                                byte instanceId = reader.ReadByte();
                                byte[] parameters = reader.ReadNextBytes(packetBuffer.Length);
                                #endregion

                                #region Logic
                                OnAutoSyncHandler(receivedPacket, viewId, instanceId, parameters, registerType);
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

        private void OnApplicationQuit()
        {
            using (TokenSource)
            {
                if (Initialized)
                {
                    Initialized = false; //* Marca o servidor como off.
                    TokenSource.Cancel(); //* Cancela todos os threads.
                    //*****************************************************
                    foreach (NeutronPlayer player in PlayersById.Values)
                        player.Dispose(); //* Libera todos os recursos não gerenciados.
                    //*****************************************************
                    _acceptedClients.Dispose();
                    _dataForProcessing.Dispose();
                    //*********************************
                    TcpListener.Stop();
                }
            }
        }
        #endregion
    }
}