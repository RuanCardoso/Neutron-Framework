using NeutronNetwork.Client.Internal;
using NeutronNetwork.Components;
using NeutronNetwork.Extensions;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Packets;
using NeutronNetwork.Server.Internal;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
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
        #region Fields
        protected readonly Queue<TaskCompletionSource<NeutronPlayer[]>> tcss = new Queue<TaskCompletionSource<NeutronPlayer[]>>();
        private GameObject _matchManager;
        public string _sceneName;
        #endregion

        #region Properties
        //* Obtém a instância de Neutron, classe derivada.
        protected Neutron This
        {
            get;
            private set;
        }

        /// <summary>
        ///* Obtém o gerenciador de física.
        /// </summary>
        public PhysicsManager PhysicsManager
        {
            get;
            protected set;
        }

        //* Mantém o estado do jogador.
        protected StateObject StateObject
        {
            get;
        } = new StateObject();

        /// <summary>
        ///* Use para obter as estatísticas de rede.
        /// </summary>
        public NetworkTime NetworkTime { get; } = new NetworkTime();

        //* Define quando o cliente está pronto para uso.
        protected bool IsReady
        {
            get;
            private set;
        }

        /// <summary>
        ///* Define se é a instância do servidor.
        /// </summary>
        public bool IsServer
        {
            get;
            private set;
        }

        /// <summary>
        ///* Obtém o tipo de cliente da instância.
        /// </summary>
        public ClientMode ClientMode
        {
            get;
            private set;
        }

        //* Server instance.......
        private Neutron Instance => Neutron.Server.Instance;
        #endregion

        #region Internal Events
        protected NeutronEventNoReturn<bool, Action, Neutron> Internal_OnNeutronConnected;
        protected NeutronEventNoReturn<bool, JObject, Action, Neutron> Internal_OnNeutronAuthenticated;
        protected NeutronEventNoReturn<NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerConnected;
        protected NeutronEventNoReturn<string, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerDisconnected;
        protected NeutronEventNoReturn<string, NeutronPlayer, bool, Action, Neutron> Internal_OnMessageReceived;
        protected NeutronEventNoReturn<NeutronChannel[], Action, Neutron> Internal_OnChannelsReceived;
        protected NeutronEventNoReturn<NeutronRoom[], Action, Neutron> Internal_OnRoomsReceived;
        protected NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerLeftChannel;
        protected NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerLeftRoom;
        protected NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerCreatedRoom;
        protected NeutronEventNoReturn<NeutronRoom, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerJoinedRoom;
        protected NeutronEventNoReturn<NeutronChannel, NeutronPlayer, bool, Action, Neutron> Internal_OnPlayerJoinedChannel;
        protected NeutronEventNoReturn<NeutronPlayer, string, bool, Action, Neutron> Internal_OnPlayerNicknameChanged;
        protected NeutronEventNoReturn<NeutronPlayer, string, bool, Action, Neutron> Internal_OnPlayerPropertiesChanged;
        protected NeutronEventNoReturn<NeutronPlayer, string, bool, Action, Neutron> Internal_OnRoomPropertiesChanged;
        protected NeutronEventNoReturn<NeutronStream.IReader, NeutronPlayer, byte, Action, Neutron> Internal_OnPlayerCustomPacketReceived;
        protected NeutronEventNoReturn<Packet, string, int, Action, Neutron> Internal_OnError;
        #endregion

        #region Internal Funcs
        //* Inicializa o cliente do servidor, o servidor também é um cliente dele mesmo.
        public void Initialize(Neutron neutron)
        {
            This = neutron;
            //* Inicia o timer do servidor....
            NetworkTime.Stopwatch.Start();
            //* Marca que a instância é do servidor.
            IsServer = true;
        }

        //* Inicializa o cliente e registra os eventos de Neutron.
        protected void Initialize(ClientMode clientMode)
        {
            This = (Neutron)this;
            ClientMode = clientMode;
            Internal_OnNeutronConnected += OnNeutronConnected;
            Internal_OnNeutronAuthenticated += OnNeutronAuthenticated;
            Internal_OnPlayerConnected += OnPlayerConnected;
            Internal_OnPlayerDisconnected += OnPlayerDisconnected;
            Internal_OnMessageReceived += OnMessageReceived;
            Internal_OnChannelsReceived += OnChannelsReceived;
            Internal_OnRoomsReceived += OnRoomsReceived;
            Internal_OnRoomPropertiesChanged += OnRoomPropertiesChanged;
            Internal_OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
            Internal_OnPlayerNicknameChanged += OnPlayerNicknameChanged;
            Internal_OnPlayerCustomPacketReceived += OnPlayerCustomPacketReceived;
            Internal_OnPlayerCreatedRoom += OnPlayerCreatedRoom;
            Internal_OnPlayerJoinedChannel += OnPlayerJoinedChannel;
            Internal_OnPlayerJoinedRoom += OnPlayerJoinedRoom;
            Internal_OnPlayerLeftChannel += OnPlayerLeftChannel;
            Internal_OnPlayerLeftRoom += OnPlayerLeftRoom;
            Internal_OnError += OnError;
            //* Define nome da cena principal do cliente.
            _sceneName = $"Client(Container) - {SceneHelper.GetSideTag(IsServer)} - [{clientMode}] - ({UnityEngine.Random.Range(1, int.MaxValue)})";
        }

        //* Client->Server
        //* Server->Client
        protected void Send(NeutronStream stream, NeutronPlayer player, Protocol protocol)
        {
            NeutronStream.IWriter writer = stream.Writer;
            if (player == null)
                Send(stream, protocol);
            else
            {
                var packet = Helper.PollPacket(writer.ToArray(), player, player, protocol);
                Neutron.Server.AddPacket(packet);
            }
        }

        //* Client->Server
        //* Server->Client
        protected void Send(NeutronStream stream, NeutronPlayer player, bool isServerSide, Protocol protocol)
        {
            NeutronStream.IWriter writer = stream.Writer;
            if (!isServerSide)
                Send(stream, protocol);
            else
            {
                var packet = Helper.PollPacket(writer.ToArray(), player, player, protocol);
                Neutron.Server.AddPacket(packet);
            }
        }

        //* Usado para enviar os dados para o servidor.
        //* Client->Server
        protected void Send(NeutronStream stream, Protocol protocol = Protocol.Tcp)
        {
            try
            {
                if (IsServer)
                    throw new NeutronException("To use this packet on the server side it is necessary to assign the \"Player\" parameter.");
#if !UNITY_SERVER || UNITY_EDITOR
                if (This.IsConnected)
                {
                    NeutronStream.IWriter wHeader = stream.hWriter;
                    NeutronStream.IWriter wPacket = stream.Writer;
                    byte[] pBuffer = wPacket.ToArray().Compress(); //! Otimizado para evitar alocações, bom isso depende de como você usa o Neutron :p
                    switch (protocol)
                    {
                        //* ValueTask ainda não funciona na Unity, isso mesmo, em 2021 com .net 6 e standard 2.1, e a unity atrasada com essa merda de Mono, vê se pode?
                        case Protocol.Tcp:
                            {
                                if (wHeader.GetPosition() == 0)
                                {
                                    wHeader.WriteByteArrayWithAutoSize(pBuffer); //* Pre-fixa o tamanho da mensagem no cabeçalho, um inteiro/short/byte(4/2/1 bytes), e a mensagem.
                                    byte[] hBuffer = wHeader.ToArray();
                                    wHeader.Write();

                                    NetworkStream networkStream = TcpClient.GetStream();
                                    switch (Helper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            networkStream.Write(hBuffer, 0, hBuffer.Length); //* Envia os dados pro servidor de modo síncrono, esta opção é melhor, não aloca e tem performance de CPU.
                                            break;
                                        default:
                                            if (Helper.GetConstants().SendAsyncPattern == AsynchronousType.APM)
                                                networkStream.Write(hBuffer, 0, hBuffer.Length); //* Envia os dados pro servidor de modo assíncrono, mentira, envia da mesma forma de "SendType.Synchronous", preguiça mesmo. :p, porque BeginReceive e Endreceive é chato de fazer pro TCP :D
                                            else
                                                SocketHelper.SendTcpAsync(networkStream, hBuffer, TokenSource.Token); //* Envia os dados pro servidor de forma assíncrona., faz alocações pra caralho, no tcp não tanto, mas no UDP..... é foda. e usa muita cpu, evite, se souber como resolver, sinta-se a vontade para contribuir.
                                            break;
                                    }
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ClientTCP.AddOutgoing(hBuffer.Length);
                                }
                                else
                                    throw new Exception($"Send(Tcp): Invalid position, is not zero! Pos -> {wHeader.GetPosition()} Capacity -> {wHeader.GetCapacity()}. You called Finish() ?");
                            }
                            break;
                        case Protocol.Udp:
                            {
                                if (UdpEndPoint == null)
                                    LogHelper.Error("Unauthenticated!");
                                else
                                {
                                    StateObject.SendDatagram = pBuffer;
                                    switch (Helper.GetConstants().SendModel)
                                    {
                                        case SendType.Synchronous:
                                            SocketHelper.SendBytes(UdpClient, pBuffer, UdpEndPoint); //* envia de modo síncrono, evita alocações e performance boa.
                                            break;
                                        default:
                                            {
                                                switch (Helper.GetConstants().SendAsyncPattern)
                                                {
                                                    case AsynchronousType.APM:
                                                        {
                                                            //* aloca, mas não tanto, boa performance.
                                                            SocketHelper.BeginSendBytes(UdpClient, pBuffer, UdpEndPoint, (ar) =>
                                                            {
                                                                SocketHelper.EndSendBytes(UdpClient, ar);
                                                            });
                                                            break;
                                                        }
                                                    default:
                                                        SocketHelper.SendUdpAsync(UdpClient, StateObject, UdpEndPoint); //* se foder, aloca pra caralho e usa cpu como a unreal engine, ValueTask poderia resolver, mas......
                                                        break;
                                                }
                                                break;
                                            }
                                    }
                                    //* Adiciona no profiler a quantidade de dados de saída(Outgoing).
                                    NeutronStatistics.ClientUDP.AddOutgoing(pBuffer.Length);
                                }
                            }
                            break;
                    }
                }
                else
                    LogHelper.Error("Non-connected socket, sending failed!");
#else
                    throw new Exception("To use this packet on the server side it is necessary to assign the \"Player\" parameter.");
#endif
            }
            catch (ObjectDisposedException) { }
            catch (SocketException) { }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
            }
        }

        //* Executa o iRPC na instância específicada.
#pragma warning disable IDE1006
        protected void iRPCHandler(byte rpcId, short viewId, byte instanceId, byte[] parameters, NeutronPlayer player, RegisterMode registerMode)
#pragma warning restore IDE1006
        {
            void Run((int, int, RegisterMode) key) //* a key do objeto, o primeiro parâmetro é o ID do jogador ou do Objeto de Rede ou 0(se for objeto de cena), e o segundo é o ID do objeto, e o terceiro é o tipo de objeto.
            {
                if (MatchmakingHelper.Server.GetNetworkObject(key, This.LocalPlayer, out NeutronView neutronView)) //* Obtém a instância que enviou o RPC para a rede.
                {
                    if (neutronView.iRPCs.TryGetValue((rpcId, instanceId), out RPCInvoker remoteProceduralCall)) //* Obtém o RPC com o ID enviado para a rede.
                    {
                        try
                        {
                            //* Executa o RPC, observe que isto não usa reflexão, reflexão é lento irmão, eu uso delegados, e a reflexão para criar os delegados em runtime no Awake do objeto, bem rápido, e funciona no IL2CPP.
                            ReflectionHelper.iRPC(parameters, remoteProceduralCall, player);
                        }
                        catch (Exception ex)
                        {
                            LogHelper.Stacktrace(ex);
                        }
                    }
                    else
                        LogHelper.Warn("Ignore this: iRpc with this Id not found.");
                }
                else
                    LogHelper.Warn("Ignore this: iRpc: Object not found.");
            }

            switch (registerMode)
            {
                case RegisterMode.Scene:
                    Run((0, viewId, registerMode));
                    break;
                case RegisterMode.Player:
                    Run((viewId, viewId, registerMode));
                    break;
                case RegisterMode.Dynamic:
                    Run((player.Id, viewId, registerMode));
                    break;
            }
        }

        //* Executa o gRPC, chamada global, não é por instâncias.
#pragma warning disable IDE1006
        protected void gRPCHandler(int id, NeutronPlayer player, byte[] parameters, bool isServer, bool isMine)
#pragma warning restore IDE1006
        {
            if (GlobalBehaviour.gRPCs.TryGetValue((byte)id, out RPCInvoker remoteProceduralCall)) //* Obtém o gRPC com o ID especificado.
            {
                try
                {
                    //* Invoca os gRPC, não usa reflexão para invocar, utilizo delegados, reflexão usado para criar os delegados no awake, isto melhora a performance em 1000%, do que invocar usando a reflexão.
                    ReflectionHelper.gRPC(player, parameters, remoteProceduralCall, isServer, isMine, This);
                }
                catch (Exception ex)
                {
                    LogHelper.Stacktrace(ex);
                }
            }
            else
                LogHelper.Error("Invalid gRPC ID, there is no attribute with this ID.");
        }

        //* Executa o AutoSync, observe que não usa reflexão e nem delegados, a invocação é direta, um metódo virtual, opte por usar isto sempre para sincronização constante, ex: movimentação.
        protected void AutoSyncHandler(NeutronPlayer player, short viewId, byte instanceId, byte[] buffer, RegisterMode registerType)
        {
            void Run((int, int, RegisterMode) key)
            {
                if (MatchmakingHelper.Server.GetNetworkObject(key, This.LocalPlayer, out NeutronView neutronView)) //* Obtém o objeto de rede com o ID específicado.
                {
                    if (neutronView.NeutronBehaviours.TryGetValue(instanceId, out NeutronBehaviour neutronBehaviour)) //* obtém a instância que está sincronizando os dados.
                    {
                        using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
                        {
                            NeutronStream.IReader reader = stream.Reader;
                            reader.SetBuffer(buffer);
                            neutronBehaviour.OnAutoSynchronization(stream, false);
                        }
                    }
                    else
                        LogHelper.Warn("Ignore this: iRpc: Auto sync behaviour not found.");
                }
                else
                    LogHelper.Warn("Ignore this: iRpc: Auto sync object not found.");
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
                    Run((player.Id, viewId, registerType));
                    break;
            }
        }

        protected void SynchronizeHandler(NeutronPlayer[] players, Action<NeutronPlayer> onEvent)
        {
            //* Atualiza os outros players para você.
            foreach (var player in players)
            {
                //* Atualiza o jogador, mas mantem a refêrencia.
                if (player.Equals(This.LocalPlayer))
                    continue;
                else
                {
                    var currentPlayer = Players[player.Id];
                    currentPlayer.Apply(player);
                    onEvent(currentPlayer);
                }
            }
        }
        #endregion

        #region Funcs
        /// <summary>
        ///* Obtém a lista de jogadores do atual Matchmaking.
        /// </summary>
        public NeutronPlayer[] GetPlayers(Func<NeutronPlayer, bool> filter = null, bool includeLocalPlayer = true)
        {
            if (filter == null)
                filter = x => true;

            var matchmaking = This.LocalPlayer.Matchmaking;
            if (matchmaking != null)
                return matchmaking.Players();
            else
            {
                if (includeLocalPlayer)
                    return Players.Values.ToArray().Where(x => x.IsConnected).Where(filter).ToArray();
                else
                    return Players.Values.ToArray().Where(x => x.IsConnected && !x.Equals(This.LocalPlayer)).Where(filter).ToArray();
            }
        }

        /// <summary>
        ///* Inicia uma chamada de preparação para a criação do seu jogador em rede. 
        /// </summary>
        /// <param name="parameters">* O escritor dos parâmetros.</param>
        /// <param name="position">* A posição inicial do spawn do objeto.</param>
        /// <param name="quaternion">* A rotação inicial do spawn do objeto.</param>
        public NeutronStream.IWriter BeginPlayer(NeutronStream parameters, Vector3 position, Quaternion quaternion)
        {
            var writer = This.Begin_gRPC(parameters);
            writer.Write(position);
            writer.Write(quaternion);
            return writer;
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que inicia a criação do seu jogador.<br/>
        ///*(Client-Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndPlayer(NeutronStream parameters, byte id)
        {
            This.End_gRPC(id, parameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) do lado do servidor, que inicia a criação do seu jogador.<br/>
        ///*(Server-Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndPlayer(NeutronStream parameters, byte id, NeutronPlayer player)
        {
            Instance.End_gRPC(id, parameters, Protocol.Tcp, player);
        }

        /// <summary>
        ///* Finaliza a chamada de criação do jogador em rede.<br/>
        /// </summary>
        public bool EndPlayer(NeutronStream.IReader parameters, out Vector3 position, out Quaternion quaternion)
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
        public NeutronStream.IWriter BeginObject(NeutronStream parameters, Vector3 position, Quaternion quaternion)
        {
            var writer = This.Begin_gRPC(parameters);
            writer.Write(position);
            writer.Write(quaternion);
            return writer;
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) que inicia a criação de um objeto de rede.<br/>
        ///*(Client-Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndObject(NeutronStream parameters, byte id)
        {
            This.End_gRPC(id, parameters, Protocol.Tcp);
        }

        /// <summary>
        ///* Envia uma chamada(gRPC) do lado do servidor que inicia a criação de um objeto de rede.<br/>
        ///*(Server-Side).
        /// </summary>
        /// <param name="parameters">* Os parâmetros que serão enviados com sua chamada.</param>
        public void EndObject(NeutronStream parameters, byte id, NeutronPlayer player)
        {
            Instance.End_gRPC(id, parameters, Protocol.Tcp, player);
        }

        /// <summary>
        ///* Finaliza a chamada de criação de um objeto de rede.<br/>
        /// </summary>
        public bool EndObject(NeutronStream.IReader parameters, out Vector3 position, out Quaternion quaternion)
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
        ///* Retorna se o jogador especificado é seu.
        /// </summary>
        /// <param name="otherPlayer">* O Jogador que será comparado.</param>
        /// <returns></returns>
        public bool IsMine(NeutronPlayer otherPlayer) => otherPlayer.Equals(This.LocalPlayer);

        private void MakeMatchmakingManager(NeutronPlayer player, Neutron neutron)
        {
            //* Cacha o match manager, para depois ser destruído.
            if (_matchManager != null)
                GameObject.Destroy(_matchManager);
            _matchManager = SceneHelper.MakeMatchmakingManager(player, false, neutron);
            //* Move a o gerenciador de sala pro seu container.
            SceneHelper.MoveToContainer(_matchManager, _sceneName);
        }
        #endregion

        #region Events
        private void OnPlayerLeftRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            MatchmakingHelper.Destroy(player);
            MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Room);
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnPlayerLeftChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            MatchmakingHelper.Destroy(player);
            MatchmakingHelper.Internal.Leave(player, MatchmakingMode.Channel);
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnPlayerNicknameChanged(NeutronPlayer player, string nickname, bool isMine, Action onEvent, Neutron neutron)
        {
            player.Nickname = nickname;
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnPlayerPropertiesChanged(NeutronPlayer player, string properties, bool isMine, Action onEvent, Neutron neutron)
        {
            player.Properties = properties;
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnRoomPropertiesChanged(NeutronPlayer player, string properties, bool isMine, Action onEvent, Neutron neutron)
        {
            This.LocalPlayer.Matchmaking.Properties = properties;
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnPlayerConnected(NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            player.IsConnected = true;
            if (isMine)
                IsReady = true;
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnNeutronAuthenticated(bool isSuccess, JObject properties, Action onEvent, Neutron neutron)
        {
            if (!isSuccess)
                LogHelper.Error("The connection was rejected because authentication failed.");
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private async void OnPlayerJoinedChannel(NeutronChannel remoteChannel, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            try
            {
                await NeutronSchedule.ScheduleTaskAsync(() =>
                {
                    if (!player.IsInChannel() && !player.IsInRoom())
                    {
                        NeutronChannel channel = neutron.NeutronChannel;
                        channel.Apply(remoteChannel);
                        player.Channel = channel;
                        player.Channel.Add(player);
                        player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                        if (Neutron.Server.MatchmakingMode == MatchmakingMode.All || Neutron.Server.MatchmakingMode == MatchmakingMode.Channel)
                        {
                            MakeMatchmakingManager(player, neutron);
                            if (player.Matchmaking.PhysicsManager == null)
                                player.Matchmaking.PhysicsManager = PhysicsManager;
                            if (isMine)
                                NeutronSceneObject.OnSceneObjectRegister(player.Channel.Owner, IsServer, PhysicsManager.Scene, MatchmakingMode.Channel, player.Channel, neutron);
                        }
                        player.Matchmaking.Owner = Players[remoteChannel.Owner.Id];
                    }
                    else
                        LogHelper.Error("You are already in a channel, call \"Leave\".");
                });
                //* Invoca os eventos registrados do cliente, após os eventos internos.
                onEvent.Invoke();
            }
            catch (Exception ex) //* Handling tasks exceptions.
            {
                LogHelper.Stacktrace(ex);
            }
        }

        private async void OnPlayerJoinedRoom(NeutronRoom remoteRoom, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            try
            {
                await NeutronSchedule.ScheduleTaskAsync(() =>
                {
                    if (player.IsInChannel() && !player.IsInRoom())
                    {
                        NeutronRoom room = neutron.NeutronRoom;
                        room.Apply(remoteRoom);
                        player.Room = room;
                        player.Room.Add(player);
                        player.Matchmaking = MatchmakingHelper.Matchmaking(player);
                        if (Neutron.Server.MatchmakingMode == MatchmakingMode.All || Neutron.Server.MatchmakingMode == MatchmakingMode.Room)
                        {
                            MakeMatchmakingManager(player, neutron);
                            if (player.Matchmaking.PhysicsManager == null)
                                player.Matchmaking.PhysicsManager = PhysicsManager;
                            if (isMine)
                                NeutronSceneObject.OnSceneObjectRegister(player.Room.Owner, IsServer, PhysicsManager.Scene, MatchmakingMode.Room, player.Room, neutron);
                        }
                        player.Matchmaking.Owner = Players[remoteRoom.Owner.Id];
                    }
                    else
                        LogHelper.Error("You are already in a room, call \"Leave\".");
                });
                //* Invoca os eventos registrados do cliente, após os eventos internos.
                onEvent.Invoke();
            }
            catch (Exception ex) //* Handling tasks exceptions.
            {
                LogHelper.Stacktrace(ex);
            }
        }

        private void OnPlayerCreatedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            //* Não é necessário nada aqui, OnJoinedRoom já faz todo o serviço.
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnRoomsReceived(NeutronRoom[] rooms, Action onEvent, Neutron neutron)
        {
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnChannelsReceived(NeutronChannel[] nChannels, Action onEvent, Neutron neutron)
        {
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnPlayerDisconnected(string reason, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            player.IsConnected = false;
            MatchmakingHelper.Destroy(player);
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnMessageReceived(string message, NeutronPlayer player, bool isMine, Action onEvent, Neutron neutron)
        {
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnNeutronConnected(bool success, Action onEvent, Neutron neutron)
        {
            if (success)
            {
                //* Inicia o timer do cliente....
                NetworkTime.Stopwatch.Start();
                //* Invoca os eventos registrados do cliente, após os eventos internos.
                onEvent.Invoke();
            }
        }

        private void OnPlayerCustomPacketReceived(NeutronStream.IReader reader, NeutronPlayer player, byte packet, Action onEvent, Neutron neutron)
        {
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }

        private void OnError(Packet packet, string message, int errorcode, Action onEvent, Neutron neutron)
        {
            LogHelper.Error($"[{packet}] -> {message} | errorCode: {errorcode}");
            //* Invoca os eventos registrados do cliente, após os eventos internos.
            onEvent.Invoke();
        }
        #endregion
    }
}