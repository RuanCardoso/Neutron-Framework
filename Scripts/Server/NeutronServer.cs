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
    [RequireComponent(typeof(NeutronMain))]
    [RequireComponent(typeof(NeutronDispatcher))]
    [RequireComponent(typeof(EventsBehaviour))]
    [RequireComponent(typeof(NeutronStatistics))]
    [DefaultExecutionOrder(ExecutionOrder.NEUTRON_SERVER)]
    public class NeutronServer : ServerPackets
    {
        #region Events
        /// <summary>
        ///* Este evento é acionando quando o servidor é iniciado.
        /// </summary>
        public static NeutronEventNoReturn OnServerStart = new NeutronEventNoReturn();
        /// <summary>
        ///* Este evento é acionado quando um jogador é conectado ao servidor.
        /// </summary>
        public static NeutronEventNoReturn<NeutronPlayer> OnPlayerConnected = new NeutronEventNoReturn<NeutronPlayer>();
        #endregion

        #region Fields
        /// <summary>
        ///* Obtém o status do servidor.
        /// </summary>
        public static bool m_Initialized;
        #endregion

        #region Collections
        //* Esta fila irá armazenar os clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        private NeutronBlockingQueue<TcpClient> m_AcceptedClients = new NeutronBlockingQueue<TcpClient>(NeutronConstants.BOUNDED_CAPACITY);
        //* Esta fila irá armazenar os pacotes recebidos dos clientes para serem desinfileirados e processados, em um único Thread(Segmento).
        public NeutronBlockingQueue<NeutronData> m_DataForProcessing = new NeutronBlockingQueue<NeutronData>(NeutronConstants.BOUNDED_CAPACITY);
        //* Esta fila irá fornecer um pool de ID's e atribuirá um ID único para cada cliente novo.
        //* Quando um cliente for desconectado seu ID será reutilizado sendo adicionado a fila novamente.
        public NeutronSafeQueue<int> m_PooledIds = new NeutronSafeQueue<int>();
        #endregion

        #region Threading
        //* Este é um token de cancelamento, ele é passado para todos os Threads, é usado para parar os Threads quando o servidor for desligado.
        private CancellationTokenSource m_CTS = new CancellationTokenSource();
        #endregion

        #region Functions
        private void Initilize()
        {
            //* Esta região irá fornecer os ID para a lista.
            #region Provider
            for (int i = 0; i < NeutronMain.Settings.GlobalSettings.MaxPlayers; i++)
                m_PooledIds.Enqueue((NeutronConstants.GENERATE_PLAYER_ID + i) + 1);
            #endregion

            //* Marca o servidor como inicializado.
            m_Initialized = true;

            #region Logger
            LogHelper.Info("The Server is ready, all protocols have been initialized.\r\n");
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
            Thread packetProcessingStackTh = new Thread((e) => PacketProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Highest,
                IsBackground = true,
                Name = "Neutron packetProcessingStackTh"
            };
            packetProcessingStackTh.Start();

            //* Este thread será dedicado a desinfileirar os novos clientes e processa-los.
            Thread clientsProcessingStackTh = new Thread((e) => ClientsProcessingStack())
            {
                Priority = System.Threading.ThreadPriority.Normal,
                IsBackground = true,
                Name = "Neutron ClientsProcessingStackTh"
            };
            clientsProcessingStackTh.Start();
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
                CancellationToken l_Token = m_CTS.Token;
                while (!l_Token.IsCancellationRequested)
                {
                    TcpClient l_Client = await TcpSocket.AcceptTcpClientAsync(); //* Aceita a nova conexão,
                    {
                        m_AcceptedClients.Add(l_Client, l_Token); //* Nova conexão é enfileirada para processamento.
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }

        //* Inicia o processamento dos clientes.
        private void ClientsProcessingStack()
        {
            try
            {
                CancellationToken l_Token = m_CTS.Token;
                while (!l_Token.IsCancellationRequested)
                {
                    TcpClient l_Client = m_AcceptedClients.Take(l_Token); //* Desinfileira os clientes e bloqueia o thread se não houver mais dados.
                    if (PlayerHelper.GetAvailableID(out int ID))
                    {
                        if (SocketHelper.LimitConnectionsByIP(l_Client)) //* Verifica se este cliente antigiu o limite de conexões ativas.
                        {
                            l_Client.NoDelay = NeutronMain.Settings.GlobalSettings.NoDelay;
                            // TODO acceptedClient.ReceiveTimeout = int.MaxValue;
                            // TODO acceptedClient.SendTimeout = int.MaxValue;
                            var l_Player = new NeutronPlayer(ID, l_Client, new CancellationTokenSource()); //* Cria uma instância do cliente.
                            if (SocketHelper.AddPlayer(l_Player))
                            {
                                Interlocked.Increment(ref CurrentPlayers); //* Incrementa a quantidade de jogadores do servidor.
                                //* Esta região cria um View, um View é usado para você criar uma comunicação personalizada com o cliente dono(owner).
                                //* Exemplo, dentro do View você pode implementar uma função que envia um evento ou mensagem a cada X Segundos.
                                #region View
                                NeutronDispatcher.Dispatch(() =>
                                {
                                    GameObject iView = new GameObject($"View[{l_Player.ID}]");
                                    View View = iView.AddComponent<View>();
                                    View.Owner = l_Player;
                                    SceneHelper.MoveToContainer(iView, "[Container] -> Server");
                                });
                                #endregion
                                LogHelper.Info($"Incoming client, IP: [{((IPEndPoint)l_Player.m_TcpClient.Client.RemoteEndPoint).Address}] | TCP: [{((IPEndPoint)l_Player.m_TcpClient.Client.RemoteEndPoint).Port}] | UDP: [{((IPEndPoint)l_Player.m_UdpClient.Client.LocalEndPoint).Port}] -:[{CurrentPlayers}]");

                                //* Usa o pool de threads para receber os dados do cliente de forma assíncrona.
                                //* O Pool de threads só deverá ser usado com metódos que não bloqueiam, para evitar gargalos no recebimento de dados.
                                ThreadPool.QueueUserWorkItem((e) =>
                                {
                                    OnReceivingData(l_Player, Protocol.Tcp);
                                    OnReceivingData(l_Player, Protocol.Udp);
                                });
                                OnPlayerConnected.Invoke(l_Player);
                            }
                            else
                            {
                                if (!LogHelper.Error("Failed to add Player!"))
                                    l_Client.Close();
                                continue;
                            }
                        }
                        else
                        {
                            if (!LogHelper.Error("Client not allowed!"))
                                l_Client.Close();
                            continue;
                        }
                    }
                    else
                    {
                        if (!LogHelper.Error("Max Players Reached"))
                            l_Client.Close();
                        continue;
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }

        //* Inicia o processamento dos pacotes.
        private void PacketProcessingStack()
        {
#if NEUTRON_DEBUG || UNITY_EDITOR
            ServerDataProcessingStackManagedThreadId = ThreadHelper.GetThreadID();
#endif
            try
            {
                CancellationToken l_Token = m_CTS.Token;
                while (!l_Token.IsCancellationRequested)
                {
                    for (int i = 0; i < NeutronMain.Settings.ServerSettings.PacketsProcessedPerTick; i++)
                    {
                        NeutronData l_Data = m_DataForProcessing.Take(l_Token); //* Desinfileira os dados e bloqueia o thread se não houver mais dados.
                        {
                            RunPacket(l_Data.Player, l_Data.Buffer); //* Processa o pacote.
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }

        //* Aqui os dados são enviados aos seus clientes.
        public async void OnSendingData(NeutronPlayer nPlayer, NeutronData nData)
        {
            try
            {
                #region Lag Simulation
                if (NeutronMain.Settings.LagSimulationSettings.Outbound)
                    await Task.Delay(NeutronMain.Settings.LagSimulationSettings.InOutDelay);
                #endregion

                CancellationToken l_Token = nPlayer.m_CTS.Token;
                NetworkStream l_NetStream = nPlayer.m_TcpClient.GetStream();

                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    int l_Length = nData.Buffer.Length;

                    //* Cabeçalho da mensagem/dados.
                    #region Header
                    nWriter.SetLength(0);
                    nWriter.WriteFixedLength(l_Length); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro(4bytes).
                    nWriter.Write(nData.Buffer); //* Os dados que serão enviados junto com o pre-fixo.
                    byte[] l_Message = nWriter.ToArray();
                    #endregion

                    switch (nData.Protocol)
                    {
                        case Protocol.Tcp:
                            {
                                await l_NetStream.WriteAsync(l_Message, 0, l_Message.Length, l_Token);
                                {
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.m_ServerTCP.AddOutgoing(l_Length);
                                }
                            }
                            break;
                        case Protocol.Udp:
                            {
                                if (nPlayer.rPEndPoint != null) //* Verifica se o IP de destino não é nulo, se o ip de destino for nulo, o servidor não enviará os dados.
                                {
                                    await nPlayer.m_UdpClient.SendAsync(nData.Buffer, l_Length, nPlayer.rPEndPoint);
                                    {
                                        //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                        NeutronStatistics.m_ServerUDP.AddOutgoing(l_Length);
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
            catch (Exception) { }
        }

        //* Recebe/Ler os dados dos clientes.
        private async void OnReceivingData(NeutronPlayer nPlayer, Protocol nProtocol)
        {
            try
            {
                CancellationToken l_Token = nPlayer.m_CTS.Token;
                NetworkStream l_NetStream = nPlayer.m_TcpClient.GetStream();

                byte[] l_HeaderBuffer = new byte[sizeof(int)]; //* aqui será armazenado o pre-fixo do cabeçalho, que é o tamanho da mensagem enviada pelo cliente.

                while (!m_CTS.Token.IsCancellationRequested && !l_Token.IsCancellationRequested)
                {
                    if (nProtocol == Protocol.Tcp)
                    {
                        if (await SocketHelper.ReadAsyncBytes(l_NetStream, l_HeaderBuffer, 0, sizeof(int), l_Token)) //* ler o pre-fixo, um inteiro, 4 bytes(sizeof(int)) e armazena no buffer.
                        {
                            int l_MessageSize = BitConverter.ToInt32(l_HeaderBuffer, 0); //* converte o buffer do pre-fixo de volta em inteiro.
                            if (l_MessageSize > MAX_RECEIVE_MESSAGE_SIZE || l_MessageSize <= 0) //* Verifica se o tamanho da mensagem é válido.
                                DisconnectHandler(nPlayer);
                            else
                            {
                                byte[] l_MessageBuffer = new byte[l_MessageSize]; //* cria um buffer com o tamanho da mensagem/pre-fixo.
                                if (await SocketHelper.ReadAsyncBytes(l_NetStream, l_MessageBuffer, 0, l_MessageSize, l_Token)) //* ler a mensagem e armazena no buffer de mensagem.
                                {
                                    m_DataForProcessing.Add(new NeutronData(l_MessageBuffer, nPlayer, Protocol.Tcp), l_Token); //* Adiciona os dados na fila para processamento.
                                    {
                                        //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                        NeutronStatistics.m_ServerTCP.AddIncoming(l_MessageSize);
                                    }
                                }
                                else
                                    DisconnectHandler(nPlayer); //* Desconecta o cliente caso a leitura falhe, a leitura falhará em caso de desconexão...etc.
                            }
                        }
                        else
                            DisconnectHandler(nPlayer);
                    }
                    else if (nProtocol == Protocol.Udp)
                    {
                        var l_Datagram = await nPlayer.m_UdpClient.ReceiveAsync(); //* Recebe os dados enviados pelo cliente.
                        if (l_Datagram.Buffer.Length > 0)
                        {
                            //* Esta região funciona como um "Handshake", o cliente envia algum pacote vazio após a conexão, após o servidor receber este pacote, atribui o ip de destino, que é para onde os dados serão enviados.
                            //! Se o ip de destino for nulo, o servidor não enviará os dados, porque não tem destino, não houve "Handshake".
                            //! A tentativa de envio sem o "Handshake" causará a exceção de "An existing connection was forcibly closed by the remote host"
                            #region Handshake
                            if (nPlayer.rPEndPoint == null) //* verifica se o ip de destino é nulo, se for, ele é atribuído com o ip de destino.
                                nPlayer.rPEndPoint = l_Datagram.RemoteEndPoint; //* ip de destino do cliente, para onde o servidor irá enviar os dados.
                            #endregion

                            m_DataForProcessing.Add(new NeutronData(l_Datagram.Buffer, nPlayer, Protocol.Udp), l_Token); //* Adiciona os dados na fila para processamento.
                            {
                                //* Adiciona no profiler a quantidade de dados de entrada(Incoming).
                                NeutronStatistics.m_ServerUDP.AddIncoming(l_Datagram.Buffer.Length);
                            }
                        }
                    }
                }
            }
            catch (ObjectDisposedException) { }
            catch (OperationCanceledException) { }
            catch (SocketException) { }
            catch (Exception ex) { LogHelper.StackTrace(ex); }
        }
        #endregion

        #region Packets
        //* Aqui os pacotes serão processados, seus parâmetros serão lidos, e executado sua respectiva função.
        void RunPacket(NeutronPlayer nSender, byte[] nData)
        {
#if UNITY_SERVER || UNITY_EDITOR
            using (NeutronReader nReader = Neutron.PooledNetworkReaders.Pull())
            {
                nReader.SetBuffer(nData);
                switch (nReader.ReadPacket<Packet>()) //* Ler o pacote recebido
                {
                    case Packet.Handshake:
                        {
                            #region Logic
                            HandshakeHandler(nSender, nReader.ReadDouble());
                            #endregion
                        }
                        break;
                    case Packet.Nickname:
                        {
                            #region Reader
                            string nickname = nReader.ReadString();
                            #endregion

                            #region Logic
                            NicknameHandler(nSender, nickname);
                            #endregion
                        }
                        break;
                    case Packet.Chat:
                        {
                            #region Defaults
                            TunnelingTo broadcast = default(TunnelingTo);
                            int networkID = default(int);
                            #endregion

                            #region Reader
                            ChatPacket chatPacket = nReader.ReadPacket<ChatPacket>();
                            switch (chatPacket)
                            {
                                case ChatPacket.Global:
                                    broadcast = nReader.ReadPacket<TunnelingTo>();
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
                    case Packet.iRPC:
                        {
                            #region Reader
                            TunnelingTo broadcast = nReader.ReadPacket<TunnelingTo>();
                            TargetTo sendTo = nReader.ReadPacket<TargetTo>();
                            Cache cacheMode = nReader.ReadPacket<Cache>();
                            Protocol recProtocol = nReader.ReadPacket<Protocol>();
                            int networkID = nReader.ReadInt32();
                            int attributeID = nReader.ReadInt32();
                            byte[] parameters = nReader.ReadExactly();
                            #endregion

                            #region Logic
                            iRPCHandler(nSender, broadcast, sendTo, cacheMode, networkID, attributeID, parameters, recProtocol);
                            #endregion
                        }
                        break;
                    case Packet.gRPC:
                        {
                            #region Reader
                            int networkID = nReader.ReadInt32();
                            int attributeID = nReader.ReadInt32();
                            byte[] parameters = nReader.ReadExactly();
                            #endregion

                            #region Logic
                            gRPCHandler(nSender, networkID, attributeID, parameters);
                            #endregion
                        }
                        break;
                    case Packet.GetChannels:
                        {
                            #region Logic
                            GetChannelsHandler(nSender);
                            #endregion
                        }
                        break;
                    case Packet.JoinChannel:
                        {
                            #region Reader
                            int channelID = nReader.ReadInt32();
                            #endregion

                            #region Logic
                            JoinChannelHandler(nSender, channelID);
                            #endregion
                        }
                        break;
                    case Packet.GetChached:
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
                    case Packet.CreateRoom:
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
                    case Packet.GetRooms:
                        {
                            #region Logic
                            GetRoomsHandler(nSender);
                            #endregion
                        }
                        break;
                    case Packet.JoinRoom:
                        {
                            #region Reader
                            int roomID = nReader.ReadInt32();
                            #endregion

                            #region Logic
                            JoinRoomHandler(nSender, roomID);
                            #endregion
                        }
                        break;
                    case Packet.Leave:
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
                    case Packet.DestroyPlayer:
                        {
                            #region Logic
                            DestroyPlayerHandler(nSender);
                            #endregion
                        }
                        break;
                    case Packet.SetPlayerProperties:
                        {
                            #region Reader
                            string properties = nReader.ReadString();
                            #endregion

                            #region Logic
                            SetPlayerPropertiesHandler(nSender, properties);
                            #endregion
                        }
                        break;
                    case Packet.SetRoomProperties:
                        {
                            #region Reader
                            string properties = nReader.ReadString();
                            #endregion

                            #region Logic
                            SetRoomPropertiesHandler(nSender, properties);
                            #endregion
                        }
                        break;
                    case Packet.Ping:
                        {
                            #region Reader
                            double time = nReader.ReadDouble();
                            #endregion

                            #region Logic
                            PingHandler(nSender, time);
                            #endregion
                        }
                        break;
                    case Packet.CustomPacket:
                        {
                            #region Defaults
                            bool isMine = false;
                            TargetTo sendTo = default(TargetTo);
                            TunnelingTo broadcast = default(TunnelingTo);
                            #endregion

                            #region Reader
                            int networkID = nReader.ReadInt32();
                            CustomPacket clientPacket = nReader.ReadPacket<CustomPacket>();
                            if ((isMine = PlayerHelper.IsMine(nSender, networkID)))
                            {
                                sendTo = nReader.ReadPacket<TargetTo>();
                                broadcast = nReader.ReadPacket<TunnelingTo>();
                            }
                            Protocol recProtocol = nReader.ReadPacket<Protocol>();
                            byte[] parameters = nReader.ReadExactly();
                            #endregion

                            #region Logic
                            ClientPacketHandler(nSender, isMine, networkID, parameters, clientPacket, sendTo, broadcast, recProtocol);
                            #endregion
                        }
                        break;
                    case Packet.OnAutoSync:
                        {
                            #region Reader
                            Protocol recProtocol = nReader.ReadPacket<Protocol>();
                            TargetTo sendTo = nReader.ReadPacket<TargetTo>();
                            TunnelingTo broadcast = nReader.ReadPacket<TunnelingTo>();
                            int networkID = nReader.ReadInt32();
                            int instanceID = nReader.ReadInt32();
                            byte[] parameters = nReader.ReadExactly();
                            #endregion

                            #region Logic
                            OnSerializeViewHandler(nSender, networkID, instanceID, parameters, sendTo, broadcast, recProtocol);
                            #endregion
                        }
                        break;
                }
            }
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
            var l_TargetGroup = BuildPipeline.GetBuildTargetGroup(EditorUserBuildSettings.activeBuildTarget);
            var l_ACL = PlayerSettings.GetApiCompatibilityLevel(l_TargetGroup);
            if (l_ACL != ApiCompatibilityLevel.NET_Standard_2_0)
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
            using (m_CTS)
            {
                if (m_Initialized)
                {
                    m_Initialized = false;
                    m_CTS.Cancel();
                    await Task.Delay(50);
                    foreach (var l_Player in PlayersById.Values)
                    {
                        l_Player.m_CTS.Cancel();
                        await Task.Delay(20);
                        l_Player.Dispose();
                    }
                    m_AcceptedClients.Dispose();
                    m_DataForProcessing.Dispose();
                    TcpSocket.Stop();
                }
            }
        }
        #endregion
    }
}