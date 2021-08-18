using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Packets;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using UnityEngine;

namespace NeutronNetwork.Helpers
{
    public static class ReflectionHelper
    {
        public static T GetAttribute<T>(string methodName, object instance) where T : Attribute
        {
            var method = GetMethod(methodName, instance);
            if (method != null)
                return method.GetCustomAttribute<T>();
            else return null;
        }

        public static T[] GetAttributes<T>(object instance) where T : Attribute
        {
            var methods = GetMethods(instance);
            if (methods.Length > 0)
            {
                List<T> attributes = new List<T>();
                for (int i = 0; i < methods.Length; i++)
                {
                    T attr = methods[i].GetCustomAttribute<T>();
                    if (attr != null)
                        attributes.Add(attr);
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else return null;
        }

        public static T[][] GetMultipleAttributes<T>(object instance) where T : Attribute
        {
            var methods = GetMethods(instance);
            if (methods.Length > 0)
            {
                List<T[]> attributes = new List<T[]>();
                for (int i = 0; i < methods.Length; i++)
                {
                    T[] attrs = methods[i].GetCustomAttributes<T>().ToArray();
                    if (attrs.Length > 0)
                        attributes.Add(attrs);
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else return null;
        }

        public static (T[], MethodInfo)[] GetMultipleAttributesWithMethod<T>(object instance) where T : Attribute
        {
            var methods = GetMethods(instance);
            if (methods.Length > 0)
            {
                List<(T[], MethodInfo)> attributes = new List<(T[], MethodInfo)>();
                for (int i = 0; i < methods.Length; i++)
                {
                    T[] attrs = methods[i].GetCustomAttributes<T>().ToArray();
                    if (attrs.Length > 0)
                        attributes.Add((attrs, methods[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else return null;
        }

        public static MethodInfo GetMethod(string name, object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetMethod(name, flags);
        }

        public static MethodInfo[] GetMethods(object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetMethods(flags);
        }

#pragma warning disable IDE1006
        public static async Task<bool> iRPC(byte[] buffer, RPCInvoker remoteProceduralCall, NeutronPlayer player)
#pragma warning restore IDE1006
        {
            NeutronReader reader = Neutron.PooledNetworkReaders.Pull();
            reader.SetBuffer(buffer);

            switch (remoteProceduralCall.Type)
            {
                case MethodType.Async | MethodType.Bool:
                    {
                        return await remoteProceduralCall.iRPCBoolAsync(reader, player);
                    }
                case MethodType.Bool:
                    {
                        return remoteProceduralCall.iRPCBool(reader, player);
                    }
                case MethodType.Async | MethodType.Task:
                    {
                        await remoteProceduralCall.iRPCTaskAsync(reader, player);
                        return true;
                    }
                case MethodType.Async | MethodType.Void:
                case MethodType.Void:
                    {
                        remoteProceduralCall.iRPCVoid(reader, player);
                        return true;
                    }
                default:
                    return LogHelper.Error($"Type not implemented!");
            }
        }

#pragma warning disable IDE1006
        public static async Task<bool> gRPC(NeutronPlayer player, byte[] buffer, RPCInvoker remoteProceduralCall, bool isServer, bool isMine, Neutron instance)
#pragma warning restore IDE1006
        {
            NeutronReader reader = Neutron.PooledNetworkReaders.Pull();
            reader.SetBuffer(buffer);

            switch (remoteProceduralCall.Type)
            {
                case MethodType.Async | MethodType.View:
                case MethodType.View:
                    {
                        NeutronView neutronView = await remoteProceduralCall.gRPCViewAsync(reader, isServer, isMine, player, instance);
                        bool result = await NeutronSchedule.ScheduleTaskAsync(() =>
                        {
                            if (neutronView.CompareTag("Player"))
                                return neutronView.OnNeutronRegister(player, isServer, RegisterMode.Player, instance);
                            else
                            {
                                using (NeutronReader idReader = Neutron.PooledNetworkReaders.Pull())
                                {
                                    idReader.SetBuffer(buffer);
                                    idReader.SetPosition((sizeof(float) * 3) + (sizeof(float) * 4));
                                    return neutronView.OnNeutronRegister(player, isServer, RegisterMode.Dynamic, instance, idReader.ReadInt16());
                                }
                            }
                        });

                        return await NeutronSchedule.ScheduleTaskAsync(() =>
                        {
                            if (!result)
                                MonoBehaviour.Destroy(neutronView.gameObject);
                            return result;
                        });
                    }
                case MethodType.Async | MethodType.Bool:
                    {
                        return await remoteProceduralCall.gRPCBoolAsync(reader, isServer, isMine, player, instance);
                    }
                case MethodType.Bool:
                    {
                        return remoteProceduralCall.gRPCBool(reader, isServer, isMine, player, instance);
                    }
                case MethodType.Async | MethodType.Int:
                    {
                        return Convert.ToBoolean(await remoteProceduralCall.gRPCIntAsync(reader, isServer, isMine, player, instance));
                    }
                case MethodType.Int:
                    {
                        return Convert.ToBoolean(remoteProceduralCall.gRPCInt(reader, isServer, isMine, player, instance));
                    }
                case MethodType.Async | MethodType.Task:
                    {
                        await remoteProceduralCall.gRPCTaskAsync(reader, isServer, isMine, player, instance);
                        return true;
                    }
                case MethodType.Async | MethodType.Void:
                case MethodType.Void:
                    {
                        remoteProceduralCall.gRPCVoid(reader, isServer, isMine, player, instance);
                        return true;
                    }
                default:
                    return LogHelper.Error($"Type not implemented!");
            }
        }
    }
}