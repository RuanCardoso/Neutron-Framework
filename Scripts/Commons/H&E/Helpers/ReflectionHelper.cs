using NeutronNetwork.Internal;
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
            else
                return null;
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
            else
                return null;
        }

        public static (T, MethodInfo)[] GetAttributesWithMethod<T>(object instance) where T : Attribute
        {
            var methods = GetMethods(instance);
            if (methods.Length > 0)
            {
                List<(T, MethodInfo)> attributes = new List<(T, MethodInfo)>();
                for (int i = 0; i < methods.Length; i++)
                {
                    T attr = methods[i].GetCustomAttribute<T>();
                    if (attr != null)
                        attributes.Add((attr, methods[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else
                return default;
        }

        public static (T, FieldInfo)[] GetAttributesWithField<T>(object instance) where T : Attribute
        {
            var fields = GetFields(instance);
            if (fields.Length > 0)
            {
                List<(T, FieldInfo)> attributes = new List<(T, FieldInfo)>();
                for (int i = 0; i < fields.Length; i++)
                {
                    T attr = fields[i].GetCustomAttribute<T>();
                    if (attr != null)
                        attributes.Add((attr, fields[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else
                return default;
        }

        public static (T, PropertyInfo)[] GetAttributesWithProperty<T>(object instance) where T : Attribute
        {
            var properties = GetProperties(instance);
            if (properties.Length > 0)
            {
                List<(T, PropertyInfo)> attributes = new List<(T, PropertyInfo)>();
                for (int i = 0; i < properties.Length; i++)
                {
                    T attr = properties[i].GetCustomAttribute<T>();
                    if (attr != null)
                        attributes.Add((attr, properties[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else
                return default;
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
            else
                return null;
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
            else
                return null;
        }

        public static (T[], FieldInfo)[] GetMultipleAttributesWithField<T>(object instance) where T : Attribute
        {
            var fields = GetFields(instance);
            if (fields.Length > 0)
            {
                List<(T[], FieldInfo)> attributes = new List<(T[], FieldInfo)>();
                for (int i = 0; i < fields.Length; i++)
                {
                    T[] attrs = fields[i].GetCustomAttributes<T>().ToArray();
                    if (attrs.Length > 0)
                        attributes.Add((attrs, fields[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else
                return null;
        }

        public static (T[], PropertyInfo)[] GetMultipleAttributesWithProperty<T>(object instance) where T : Attribute
        {
            var properties = GetProperties(instance);
            if (properties.Length > 0)
            {
                List<(T[], PropertyInfo)> attributes = new List<(T[], PropertyInfo)>();
                for (int i = 0; i < properties.Length; i++)
                {
                    T[] attrs = properties[i].GetCustomAttributes<T>().ToArray();
                    if (attrs.Length > 0)
                        attributes.Add((attrs, properties[i]));
                    else
                        continue;
                }
                return attributes.ToArray();
            }
            else
                return null;
        }

        public static MethodInfo GetMethod(string name, object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetMethod(name, flags);
        }

        public static MethodInfo[] GetMethods(object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetMethods(flags);
        }

        public static FieldInfo GetField(string name, object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetField(name, flags);
        }

        public static FieldInfo[] GetFields(object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetFields(flags);
        }

        public static PropertyInfo GetProperty(string name, object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetProperty(name, flags);
        }

        public static PropertyInfo[] GetProperties(object instance, BindingFlags flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance)
        {
            return instance.GetType().GetProperties(flags);
        }

#pragma warning disable IDE1006
        public static async Task<bool> iRPC(byte[] buffer, RPCInvoker remoteProceduralCall, NeutronPlayer player)
#pragma warning restore IDE1006
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
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
                        return LogHelper.Error($"iRPC: Type not implemented!");
                }
            }
        }

#pragma warning disable IDE1006
        public static async Task<bool> gRPC(NeutronPlayer player, byte[] buffer, RPCInvoker remoteProceduralCall, bool isServer, bool isMine, Neutron instance)
#pragma warning restore IDE1006
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader.SetBuffer(buffer);
                switch (remoteProceduralCall.Type)
                {
                    case MethodType.Async | MethodType.View:
                    case MethodType.View:
                        {
                            NeutronView neutronView = await remoteProceduralCall.gRPCViewAsync(reader, isServer, isMine, player, instance);
                            if (neutronView != null)
                            {
                                bool result = await NeutronSchedule.ScheduleTaskAsync(() =>
                                {
                                    if (neutronView.CompareTag("Player"))
                                        return neutronView.OnNeutronRegister(player, isServer, RegisterMode.Player, instance);
                                    else
                                    {
                                        int lastPos = (sizeof(float) * 3) + (sizeof(float) * 4); //* Obtém a posição do Id do Objeto, pulando a posição(vec3) e a rotação(quat) no buffer.
                                        byte[] bufferId = new byte[sizeof(short)] //* cria uma matriz para armazenar o Id que é um short.
                                        {
                                           buffer[lastPos], //* Obtém o primeiro byte a partir da posição.
                                           buffer[lastPos + 1] //* Obtém o segundo byte a partir da posição atual + 1.
                                        };
                                        short objectId = BitConverter.ToInt16(bufferId, 0); //* Converte a matriz para o valor do tipo short(Int16).
                                                                                            //* Registra o objeto(NeutronView) na rede.
                                        return neutronView.OnNeutronRegister(player, isServer, RegisterMode.Dynamic, instance, objectId);
                                    }
                                });

                                return await NeutronSchedule.ScheduleTaskAsync(() =>
                                {
                                    if (!result)
                                        MonoBehaviour.Destroy(neutronView.gameObject);
                                    return result;
                                });
                            }
                            else
                                return true;
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
                        return LogHelper.Error($"gRPC: Type not implemented!");
                }
            }
        }
    }
}