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
        public static void iRPC(byte[] buffer, RPCInvoker remoteProceduralCall, NeutronPlayer player)
#pragma warning restore IDE1006
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader._autoDispose = true;
                reader.SetBuffer(buffer);
                switch (remoteProceduralCall.Type)
                {
                    case MethodType.Async | MethodType.Void:
                    case MethodType.Void:
                        remoteProceduralCall.iRPCVoid(reader, player);
                        break;
                    default:
                        LogHelper.Error($"iRPC: Type not implemented!");
                        break;
                }
            }
        }

#pragma warning disable IDE1006
        public static void gRPC(NeutronPlayer player, byte[] buffer, RPCInvoker remoteProceduralCall, bool isServer, bool isMine, Neutron instance)
#pragma warning restore IDE1006
        {
            using (NeutronStream stream = Neutron.PooledNetworkStreams.Pull())
            {
                NeutronStream.IReader reader = stream.Reader;
                reader._autoDispose = true;
                reader.SetBuffer(buffer);
                switch (remoteProceduralCall.Type)
                {
                    case MethodType.Async | MethodType.Void:
                    case MethodType.Void:
                        remoteProceduralCall.gRPCVoid(reader, isServer, isMine, player, instance);
                        break;
                    default:
                        LogHelper.Error($"gRPC: Type not implemented!");
                        break;
                }
            }
        }
    }
}