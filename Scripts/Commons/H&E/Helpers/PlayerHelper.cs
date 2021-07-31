using NeutronNetwork.Constants;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using System;
using System.Threading.Tasks;

namespace NeutronNetwork.Helpers
{
    public static class PlayerHelper
    {
#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public static bool iRPC(byte[] parameters, bool isMine, RPC remoteProceduralCall, NeutronPlayer player)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            var reader = Neutron.PooledNetworkReaders.Pull();
            reader.SetBuffer(parameters);

            object obj = remoteProceduralCall.Invoke(reader, isMine, player);
            if (obj != null)
            {
                Type objType = obj.GetType();
                if (objType == typeof(bool))
                    return (bool)obj;
            }
            return true;
        }

#pragma warning disable IDE1006 // Estilos de Nomenclatura
        public static async Task<bool> gRPC(int id, NeutronPlayer player, byte[] buffer, RPC remoteProceduralCall, bool isServer, bool isMine, Neutron instance = null)
#pragma warning restore IDE1006 // Estilos de Nomenclatura
        {
            NeutronReader reader = Neutron.PooledNetworkReaders.Pull();
            reader.SetBuffer(buffer);

            object method = await remoteProceduralCall.Invoke(reader, isServer, isMine, player, instance);
            switch (remoteProceduralCall.Type)
            {
                case MethodType.Async | MethodType.View:
                case MethodType.View:
                    {
                        NeutronView neutronView = (NeutronView)method;
                        return await NeutronSchedule.ScheduleTaskAsync<bool>(() =>
                        {
                            if (id == Settings.CREATE_PLAYER)
                                return neutronView.OnNeutronRegister(player, isServer, RegisterType.Player, instance);
                            else if (id == Settings.CREATE_OBJECT)
                            {
                                using (NeutronReader idReader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    idReader.SetBuffer(buffer);
                                    idReader.SetPosition((sizeof(float) * 3) + (sizeof(float) * 4));
                                    return neutronView.OnNeutronRegister(player, isServer, RegisterType.Dynamic, instance, idReader.ReadInt16());
                                }
                            }
                            else
                                return LogHelper.Error($"ID not implemented!");
                        });
                    }
                case MethodType.Async | MethodType.Bool:
                case MethodType.Bool:
                    return (bool)method;
                case MethodType.Async | MethodType.Int:
                case MethodType.Int:
                    return Convert.ToBoolean((int)method);
                case MethodType.Async | MethodType.Void:
                case MethodType.Void:
                    return true;
                default:
                    return LogHelper.Error($"Type not implemented!");
            }
        }

        public static void Disconnect(NeutronPlayer player, string reason)
        {
            using (NeutronWriter writer = Neutron.PooledNetworkWriters.Pull())
            {
                writer.WritePacket(Packet.Disconnection);
                writer.Write(player.ID);
                writer.Write(reason);
                player.Write(writer, OthersHelper.GetDefaultHandler().OnPlayerDisconnected);
            }
        }

        public static bool IsMine(NeutronPlayer player, int viewId)
        {
            return player.ID == viewId;
        }

        public static bool GetAvailableID(out int id)
        {
            if (Neutron.Server._pooledIds.Count > 0)
            {
                if (!Neutron.Server._pooledIds.TryDequeue(out id))
                    id = 0;
            }
            else
                id = 0;
            return id > Settings.GENERATE_PLAYER_ID;
        }
    }
}