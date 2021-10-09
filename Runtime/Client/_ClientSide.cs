using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Packets;
using Newtonsoft.Json.Linq;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrder.NEUTRON_CONNECTION)]
public abstract class ClientSide : GlobalBehaviour
{
    /// <summary>
    ///* A quantidade de jogadores virtuais.
    /// </summary>
    protected virtual int VirtualPlayerCount { get; }
    /// <summary>
    ///* Define se a conexão do cliente principal deve ser iniciada automaticamente.
    /// </summary>
    protected virtual bool AutoStartConnection { get; } = true;
    /// <summary>
    ///* Retorna se é o servidor, sempre falso no Editor.
    /// </summary>
#if UNITY_SERVER && !UNITY_EDITOR
    protected bool IsServer { get; } = true;
#else
    protected bool IsServer { get; } = false;
#endif

    /// <summary>
    ///* Inicia a conexão, chamado automaticamente se "AutoStartConnection" é true;
    /// </summary>
    protected void Connect(int index = 0, int timeout = 3, Authentication authentication = null)
    {
        Neutron neutron = Neutron.Client ?? Neutron.Create();
        if (!neutron.IsConnected)
            Register(neutron);
        neutron.Connect(index, timeout, authentication);
    }

    //* Registra os eventos da instância.
    private void Register(Neutron instance)
    {
        instance.OnChannelsReceived += OnChannelsReceived;
        instance.OnError += OnError;
        instance.OnMessageReceived += OnMessageReceived;
        instance.OnNeutronAuthenticated += OnNeutronAuthenticated;
        instance.OnNeutronConnected += OnNeutronConnected;
        instance.OnPlayerConnected += OnPlayerConnected;
        instance.OnPlayerCreatedRoom += OnPlayerCreatedRoom;
        instance.OnPlayerCustomPacketReceived += OnPlayerCustomPacketReceived;
        instance.OnPlayerDisconnected += OnPlayerDisconnected;
        instance.OnPlayerJoinedChannel += OnPlayerJoinedChannel;
        instance.OnPlayerJoinedRoom += OnPlayerJoinedRoom;
        instance.OnPlayerLeftChannel += OnPlayerLeftChannel;
        instance.OnPlayerLeftRoom += OnPlayerLeftRoom;
        instance.OnPlayerNicknameChanged += OnPlayerNicknameChanged;
        instance.OnPlayerPropertiesChanged += OnPlayerPropertiesChanged;
        instance.OnRoomPropertiesChanged += OnRoomPropertiesChanged;
        instance.OnRoomsReceived += OnRoomsReceived;
    }

    private void CreateVirtualClients()
    {
        for (int i = 0; i < VirtualPlayerCount; i++)
        {
            Neutron neutron = Neutron.Create(ClientMode.Virtual);
            Register(neutron);
            neutron.Connect();
        }
    }

    /// <summary>
    ///* Ao substituir, implemente "base.Start();"
    /// </summary>
    protected virtual void Start()
    {
        if (AutoStartConnection)
            Connect();
        CreateVirtualClients();
    }

    protected virtual void OnRoomsReceived(NeutronRoom[] rooms, Neutron neutron) { }

    protected virtual void OnRoomPropertiesChanged(NeutronPlayer player, string properties, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerPropertiesChanged(NeutronPlayer player, string properties, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerNicknameChanged(NeutronPlayer player, string nickname, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerLeftRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerLeftChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerJoinedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerJoinedChannel(NeutronChannel channel, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerDisconnected(string reason, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerCustomPacketReceived(NeutronStream.IReader reader, NeutronPlayer player, byte packetId, Neutron neutron) { }

    protected virtual void OnPlayerCreatedRoom(NeutronRoom room, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnPlayerConnected(NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnNeutronConnected(bool isSuccess, Neutron neutron) { }

    protected virtual void OnNeutronAuthenticated(bool isSuccess, JObject properties, Neutron neutron) { }

    protected virtual void OnMessageReceived(string message, NeutronPlayer player, bool isMine, Neutron neutron) { }

    protected virtual void OnError(Packet packet, string error, int errorCode, Neutron neutron) { }

    protected virtual void OnChannelsReceived(NeutronChannel[] channels, Neutron neutron) { }
}