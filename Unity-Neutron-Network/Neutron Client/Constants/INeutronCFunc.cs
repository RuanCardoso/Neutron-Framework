using System;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using UnityEngine;

public class NeutronCFunc : NeutronCConst
{
    protected Neutron _;
    /// <summary>
    /// Initializes the client and activates the response events.
    /// </summary>
    /// <param name="isBot">Tells whether the virtual player should behave like a bot.</param>
    protected void InitializeClient(ClientType type)
    {
        _ = (Neutron)this;
        _.isBot = (type == ClientType.Bot);
        //--------------------------------------------------------------
        _.onNeutronConnected += OnConnected;
        _.onPlayerJoinedChannel += OnPlayerJoinedChannel;
        _.onPlayerJoinedChannel += OnPlayerJoinedChannel;
        _.onPlayerJoinedRoom += OnPlayerJoinedRoom;
        _.onPlayerLeftChannel += OnPlayerLeftChannel;
        _.onPlayerLeftRoom += OnPlayerLeftRoom;
        _.onFailed += OnFailed;
        _.onNeutronDisconnected += OnDisconnected;
        _.onCreatedRoom += OnCreatedRoom;
    }

    protected void Send(byte[] buffer, ProtocolType protocolType = ProtocolType.Tcp)
    {
        buffer = buffer.Compress(COMPRESSION_MODE);
        switch (protocolType)
        {
            case ProtocolType.Tcp:
                SendTCP(buffer);
                break;
            case ProtocolType.Udp:
                SendUDP(buffer);
                break;
        }
    }

    protected async void SendTCP(byte[] buffer)
    {
        if (_TCPSocket == null) return;
        NetworkStream networkStream = _TCPSocket.GetStream();
        try
        {
            using (NeutronWriter writerOnly = new NeutronWriter())
            {
                writerOnly.WriteFixedLength(buffer.Length);
                writerOnly.Write(buffer);
                byte[] nBuffer = writerOnly.ToArray();
                //----------------------------------------------------------------------------
                await networkStream.WriteAsync(nBuffer, 0, nBuffer.Length);
                //else await networkStream.WriteAsync(buffer, 0, buffer.Length);
            }
        }
        catch (Exception ex) { Utils.LoggerError(ex.Message); }
    }

    protected void SendUDP(byte[] message)
    {
        if (_UDPSocket == null || UDPEndpoint == null) return;
        try
        {
            _UDPSocket.BeginSend(message, message.Length, UDPEndpoint, (e) =>
            {
                int data = ((UdpClient)(e.AsyncState)).EndSend(e);
                if (data > 0) { }
            }, _UDPSocket);
        }
        catch (Exception ex) { Utils.LoggerError(ex.Message); }
    }

    //============================================================================================================//
    protected bool InitConnect()
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.Connected);
            writer.Write(_.isBot);
            Send(writer.ToArray());
        }
        return true;
    }
    protected void InternalRPC(NeutronBehaviour mThis, int RPCID, object[] parameters, SendTo sendTo, bool cached, ProtocolType protocolType, Broadcast broadcast)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            object[] bArray = { mThis.ClientView.neutronProperty.ownerID, parameters };
            //---------------------------------------------------------------------------------------------------------------------
            writer.WritePacket(Packet.RPC);
            writer.WritePacket(broadcast);
            writer.Write(RPCID);
            writer.WritePacket(sendTo);
            writer.Write(cached);
            writer.Write(bArray.Serialize());
            //---------------------------------------------------------------------------------------------------------------------
            Send(writer.ToArray(), protocolType);
        }
    }

    protected void InternalRCC(MonoBehaviour mThis, int RCCID, byte[] parameters, bool enableCache, SendTo sendTo, ProtocolType protocolType, Broadcast broadcast)
    {
        using (NeutronWriter writer = new NeutronWriter())
        {
            writer.WritePacket(Packet.RCC);
            writer.WritePacket(broadcast);
            writer.Write(RCCID);
            writer.WritePacket(sendTo);
            writer.Write(mThis.GetType().Name);
            writer.Write(enableCache);
            writer.Write(parameters);
            //---------------------------------------------------------------------------------------------------------------------
            Send(writer.ToArray(), protocolType);
        }
    }
    //---------------------------------------------------------------------------------------------------------------------
    protected void HandleConnected(string status, int uniqueID, bool isBot)
    {
        Utils.Logger(status);
        //-------------------------------------------------------
        _.myPlayer = new Player(uniqueID, null, null);
        _.myPlayer.isBot = isBot;
        //-------------------------------------------------------
        if (_.onNeutronConnected != null)
        {
            new Action(() =>
            {
                _.onNeutronConnected(true, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onNeutronConnected event not registered.");
    }
    protected void HandleDisconnect(string reason)
    {
        if (_.onNeutronDisconnected != null)
        {
            _.onNeutronDisconnected(reason, _);
        }
        else Utils.LoggerError("onNeutronDisconnected event not registered.");
    }
    protected void HandleSendChat(string message, byte[] sender)
    {
        if (_.onMessageReceived != null)
        {
            new Action(() =>
            {
                _.onMessageReceived(message, sender.DeserializeObject<Player>(), _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onMessageReceived event not registered.");
    }
    //protected void HandleInstantiate (Vector3 pos, Quaternion rot, string playerPrefab, byte[] mPlayer) {
    //    //---------------------------------------------------------------------------------------------------------------------//
    //    Player playerInstantiated = mPlayer.DeserializeObject<Player> ();
    //    //---------------------------------------------------------------------------------------------------------------------//
    //    new Action (() => {
    //        GameObject playerPref = Resources.Load (playerPrefab, typeof (GameObject)) as GameObject;
    //        if (playerPref != null) {
    //            Neutron.onPlayerInstantiated (playerInstantiated, pos, rot, playerPref);
    //        } else Utils.LoggerError ($"CLIENT: -> Unable to load prefab {playerPrefab}", true);
    //    }).ExecuteOnMainThread (_, false);
    //}

    protected void HandleSendInput(byte[] mInput)
    {
        //    SerializableInput nInput = mInput.DeserializeObject<SerializableInput> ();
        //    //===================================================================
        //    SerializableVector3 nVelocity = nInput.Vector;
        //    //===================================================================
        //    Vector3 velocity = new Vector3 (nVelocity.x, nVelocity.y, nVelocity.z);
        //    //===================================================================
        //    //Neutron.Enqueue(() => playerRB.velocity = velocity);
    }

    protected void HandleRPC(int id, byte[] parameters)
    {
        object[] _array = parameters.DeserializeObject<object[]>();
        //-----------------------------------------------------------------------------
        int senderID = (int)_array[0];
        object[] objectParams = (object[])_array[1];
        //-----------------------------------------------------------------------------
        if (_.isBot) return;
        //-----------------------------------------------------------------------------
        Action RPCAction = null;
        RPCAction = new Action(() =>
        {
            if (neutronObjects.TryGetValue(senderID, out ClientView neutronObject))
            {
                Communication.InitRPC(id, objectParams, neutronObject);
            }
            else
            {
                if (id == 1005)
                {
                    Utils.Enqueue(RPCAction, ref monoBehaviourRPCActions);
                }
                //else Utils.LoggerError($"RPC[{id}]: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
            }
        });
        //-------------------------------------------------------------
        Utils.Enqueue(RPCAction, ref monoBehaviourRPCActions);
    }

    protected void HandleRCC(string monoBehaviour, int executeID, object[] objs, bool isServer)
    {
        Player sender = (Player)objs[0];
        byte[] pParams = (byte[])objs[1];
        new Action(() =>
        {
            Communication.InitRCC(monoBehaviour, executeID, sender, pParams, isServer, _);
        }).ExecuteOnMainThread(_, false);
    }

    protected void HandleACC(string monoBehaviour, int executeID, byte[] pParams)
    {
        new Action(() =>
        {
            Communication.InitACC(monoBehaviour, executeID, pParams);
        }).ExecuteOnMainThread(_, false);
    }

    protected void HandleAPC(int executeid, byte[] parameters, int playerID)
    {
        //-----------------------------------------------------------------------------
        if (_.isBot) return;
        //-----------------------------------------------------------------------------
        Action action = new Action(() =>
        {
            if (neutronObjects.TryGetValue(playerID, out ClientView neutronObject))
            {
                Communication.InitAPC(executeid, parameters, neutronObject);
            }
            //else Utils.LoggerError("APC: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
        });
        //-----------------------------------------------------------------------------
        Utils.Enqueue(action, ref monoBehaviourRPCActions);
    }

    protected void HandleDatabase(Packet packet, object[] response)
    {
        if (_.onDatabasePacket != null)
        {
            new Action(() =>
            {
                _.onDatabasePacket(packet, response, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onDatabasePacket event not registered.");
    }
    protected void HandleGetChannels(byte[] mChannels)
    {
        if (_.onChannelsReceived != null)
        {
            new Action(() =>
            {
                _.onChannelsReceived(mChannels.DeserializeObject<Channel[]>(), _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onChannelsReceived event not registered.");
    }
    protected void HandleJoinChannel(byte[] Player)
    {
        Player playerJoined = Player.DeserializeObject<Player>();
        if (_.onPlayerJoinedChannel != null)
        {
            new Action(() =>
            {
                _.onPlayerJoinedChannel(playerJoined, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onPlayerJoinedChannel event not registered.");
    }
    protected void HandleCreateRoom(Room room)
    {
        if (_.onCreatedRoom != null)
        {
            new Action(() =>
            {
                _.onCreatedRoom(room, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onCreatedRoom event not registered.");
    }
    protected void HandleGetRooms(Room[] room)
    {
        if (_.onRoomsReceived != null)
        {
            new Action(() =>
            {
                _.onRoomsReceived(room, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onRoomsReceived event not registered.");
    }
    protected void HandleFail(Packet packet, string error)
    {
        if (_.onFailed != null)
        {
            _.onFailed(packet, error, _);
        }
        else Utils.LoggerError("onFailed event not registered.");
    }
    protected void HandleJoinRoom(byte[] player)
    {
        Player playerJoined = player.DeserializeObject<Player>();
        if (_.onPlayerJoinedRoom != null)
        {
            new Action(() =>
            {
                _.onPlayerJoinedRoom(playerJoined, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onPlayerJoinedRoom event not registered.");
    }
    protected void HandleLeaveRoom(byte[] player)
    {
        Player playerLeft = player.DeserializeObject<Player>();
        if (_.onPlayerLeftRoom != null)
        {
            new Action(() =>
            {
                _.onPlayerLeftRoom(playerLeft, _);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onPlayerLeftRoom event not registered.");
    }
    protected void HandleLeaveChannel(byte[] player)
    {
        Player playerLeft = player.DeserializeObject<Player>();
        if (_.onPlayerLeftChannel != null)
        {
            new Action(() =>
            {
                _.onPlayerLeftChannel(playerLeft);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onPlayerLeftChannel event not registered.");
    }
    protected void HandleDestroyPlayer()
    {
        if (_.onDestroyed != null)
        {
            new Action(() =>
            {
                _.onDestroyed(_);
            }).ExecuteOnMainThread(_, false);
        }
        else Utils.LoggerError("onDestroyed event not registered.");
    }
    protected void HandlePlayerDisconnected(byte[] player)
    {
        Player playerDisconnected = player.DeserializeObject<Player>();
        //-----------------------------------------------------------------------------------------
        if (_.isBot) return;
        //-----------------------------------------------------------------------------------------
        if (neutronObjects.TryGetValue(playerDisconnected.ID, out ClientView neutronObject))
        {
            ClientView obj = neutronObject;
            //-----------------------------------------------------------------------------------
            new Action(() =>
            {
                MonoBehaviour.Destroy(obj.gameObject);
            }).ExecuteOnMainThread(_, false);
            //------------------------------------------------------------------------------------
            neutronObjects.TryRemove(obj.neutronProperty.ownerID, out ClientView objRemoved);
        }
        //else Utils.LoggerError("HPD: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
    }
    protected void HandleJsonProperties(int ownerID, string properties)
    {
        //----------------------------------------------------------------------------------
        if (_.isBot) return;
        //----------------------------------------------------------------------------------
        if (neutronObjects.TryGetValue(ownerID, out ClientView neutronObject))
        {
            ClientView obj = neutronObject;
            //-----------------------------------------------------------------------------------------------------------\\
            if (obj.neutronProperty != null) JsonUtility.FromJsonOverwrite(properties, obj.neutronSyncBehaviour);
            else Utils.LoggerError("It was not possible to find a class that inherits from NeutronSync Behavior.");
        }
        //else Utils.LoggerError("HJP: An attempt was made to call a method on a local player who was not yet ready.  Most common cause: Server sending data before the local player is instantiated.");
    }

    private void InitializeContainer()
    {
        GameObject container = new GameObject((!_.myPlayer.isBot) ? $"[Container] -> {_.myPlayer.ID}" : "[BOT Container]");
        //-------------------------------------------------------------------------------------------------------------------
        _.Container = container;
    }

    private void OnFailed(Packet packet, string errorMessage, Neutron localinstance)
    {
        Utils.LoggerError(packet + ":-> " + errorMessage);
    }

    private void OnPlayerJoinedChannel(Player player, Neutron localinstance)
    {
        UpdateLocalPlayer(player); // Update currentChannel
    }

    private void OnPlayerJoinedRoom(Player player, Neutron localinstance)
    {
        UpdateLocalPlayer(player); // update currentRoom
    }

    private void OnCreatedRoom(Room room, Neutron localinstance)
    {
        _.myPlayer.currentRoom = room.ID;
    }

    private void OnDisconnected(string reason, Neutron localinstance)
    {
        _.Dispose();
        //-------------------------------------------------------------------
        Utils.Logger("You Have Disconnected from server -> [" + reason + "]");
    }

    private void UpdateLocalPlayer(Player player)
    {
        if (_.isLocalPlayer(player))
        {
            _.myPlayer = player;
        }
    }

    private void OnConnected(bool success, Neutron localinstance)
    {
        if(success) InitializeContainer();
    }

    private void OnPlayerLeftRoom(Player player, Neutron localInstance)
    {

    }

    private void OnPlayerLeftChannel(Player player)
    {

    }
}