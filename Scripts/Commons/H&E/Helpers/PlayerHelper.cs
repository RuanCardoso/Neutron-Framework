using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;

namespace NeutronNetwork.Helpers
{
    public static class PlayerHelper
    {
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public static bool iRPC(byte[] parameters, bool isMine, RPC remoteProceduralCall, NeutronPlayer sender, NeutronView neutronView)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            var pool = Neutron.PooledNetworkReaders.Pull();
            pool.SetBuffer(parameters);

            object obj = remoteProceduralCall.Invoke(pool, isMine, sender);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(bool))
                    return (bool)obj;
            }
            return true;
        }

#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public static bool gRPC(int sRPCId, NeutronPlayer sender, byte[] parameters, RPC remoteProceduralCall, bool isServer, bool isMine, Neutron localInstance = null)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            var pool = Neutron.PooledNetworkReaders.Pull();
            pool.SetBuffer(parameters);

            object obj = remoteProceduralCall.Invoke(pool, isServer, isMine, sender, localInstance);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(NeutronView))
                {
                    NeutronView objectToInst = (NeutronView)obj;
                    if (!isServer)
                        SceneHelper.MoveToContainer(objectToInst.gameObject, "[Container] -> Player[Main]");
                    else
                    {
                        if (!sender.IsInRoom())
                            SceneHelper.MoveToContainer(objectToInst.gameObject, $"[Container] -> Channel[{sender.CurrentChannel}]");
                        else if (sender.IsInChannel()) SceneHelper.MoveToContainer(objectToInst.gameObject, $"[Container] -> Room[{sender.CurrentRoom}]");
                    }
                    if (sRPCId == NeutronConstants.CREATE_PLAYER)
                        NeutronRegister.RegisterPlayer(sender, objectToInst, isServer, localInstance);
                    else if (sRPCId == NeutronConstants.CREATE_OBJECT)
                    {
                        using (NeutronReader defaultOptions = Neutron.PooledNetworkReaders.Pull())
                        {
                            defaultOptions.SetBuffer(parameters);
                            defaultOptions.SetPosition((sizeof(float) * 3) + (sizeof(float) * 4));
                            NeutronRegister.RegisterObject(sender, objectToInst, defaultOptions.ReadInt32(), isServer, localInstance);
                        }
                    }
                    else return true;
                }
                else if (objType == typeof(bool))
                    return (bool)obj;
                else LogHelper.Error("invalid type hehehe");
            }
            //else NeutronLogger.LoggerError("Invalid gRPC ID, there is no attribute with this ID.");
            return true;
        }

        public static void Disconnect(NeutronPlayer nPlayer, string reason)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.Disconnection);
                writer.Write(nPlayer.ID);
                writer.Write(reason);
                nPlayer.Send(writer, NeutronMain.Synchronization.DefaultHandlers.OnPlayerDisconnected);
            }
        }

        public static void Message(NeutronPlayer nSocket, Packet packet, string message)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.SetLength(0);
                writer.WritePacket(Packet.Fail);
                writer.WritePacket(packet);
                writer.Write(message);
                nSocket.Send(writer);
            }
        }

        public static string GetNickname(int ID)
        {
            return $"Player#{ID}";
        }

        public static bool IsMine(NeutronPlayer nSender, int networkID)
        {
            return nSender.ID == networkID;
        }

        public static bool GetAvailableID(out int ID)
        {
            #region Provider
            if (Neutron.Server.m_PooledIds.Count > 0)
            {
                if (!Neutron.Server.m_PooledIds.TryDequeue(out ID))
                    ID = 0;
            }
            else ID = 0;
            #endregion

            return ID > NeutronConstants.GENERATE_PLAYER_ID;
        }
    }
}