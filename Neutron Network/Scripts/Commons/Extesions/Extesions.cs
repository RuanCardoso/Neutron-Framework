using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Reflection;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using UnityEngine;

namespace NeutronNetwork.Internal.Extesions
{
    public static class Extesions
    {
        public static void ExecuteOnMainThread(this Action action) => Utils.Enqueue(action, Neutron.Server.ActionsDispatcher);
        public static void ExecuteOnMainThread(this Action action, Neutron _) => Utils.Enqueue(action, _.mainThreadActions);
        public static byte[] Serialize(this object message)
        {
            try
            {
                Serialization serializationMode = NeutronConfig.Settings.GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case Serialization.Json:
                        string jsonString = JsonConvert.SerializeObject(message);
                        using (NeutronWriter jsonWriter = new NeutronWriter())
                        {
                            jsonWriter.Write(jsonString);
                            return jsonWriter.ToArray().Compress(NeutronConfig.Settings.GlobalSettings.Compression);
                        }
                    case Serialization.BinaryFormatter:
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream mStream = new MemoryStream())
                        {
                            formatter.Serialize(mStream, message);
                            return mStream.ToArray().Compress(NeutronConfig.Settings.GlobalSettings.Compression);
                        }
                    default:
                        return null;
                }
            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); return null; }
        }
        public static T DeserializeObject<T>(this byte[] message)
        {
            message = message.Decompress(NeutronConfig.Settings.GlobalSettings.Compression);
            try
            {
                Serialization serializationMode = NeutronConfig.Settings.GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case Serialization.Json:
                        using (NeutronReader reader = new NeutronReader(message))
                        {
                            return JsonConvert.DeserializeObject<T>(reader.ReadString());
                        }
                    case Serialization.BinaryFormatter:
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream mStream = new MemoryStream(message))
                        {
                            return (T)formatter.Deserialize(mStream);
                        }
                    default:
                        return default;
                }
            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); return default; }
        }

        public static byte[] Compress(this byte[] data, Compression compressionType)
        {
            if (compressionType == Compression.Deflate)
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(output, System.IO.Compression.CompressionLevel.Optimal))
                    {
                        dstream.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }
            else if (compressionType == Compression.Gzip)
            {
                if (data == null)
                    throw new ArgumentNullException("inputData must be non-null");

                using (var compressIntoMs = new MemoryStream())
                {
                    using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                        CompressionMode.Compress), 64 * 1024))
                    {
                        gzs.Write(data, 0, data.Length);
                    }
                    return compressIntoMs.ToArray();
                }
            }
            else return data;
        }

        public static byte[] Decompress(this byte[] data, Compression compressionType)
        {
            if (compressionType == Compression.Deflate)
            {
                using (MemoryStream input = new MemoryStream(data))
                {
                    using (MemoryStream output = new MemoryStream())
                    {
                        using (DeflateStream dstream = new DeflateStream(input, CompressionMode.Decompress))
                        {
                            dstream.CopyTo(output);
                        }
                        return output.ToArray();
                    }
                }
            }
            else if (compressionType == Compression.Gzip)
            {
                if (data == null)
                    throw new ArgumentNullException("inputData must be non-null");

                using (var compressedMs = new MemoryStream(data))
                {
                    using (var decompressedMs = new MemoryStream())
                    {
                        using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                            CompressionMode.Decompress), 64 * 1024))
                        {
                            gzs.CopyTo(decompressedMs);
                        }
                        return decompressedMs.ToArray();
                    }
                }
            }
            else return data;
        }

        public static byte[] StringToByteArray(this String hex)
        {
            int NumberChars = hex.Length;
            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static bool IsConnected(this TcpClient socket)
        {
            try
            {
                return !(socket.Client.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public static void Send(this Player mSender, SendTo sendTo, byte[] buffer, Broadcast broadcast, Protocol protocolType)
        {
            switch (protocolType)
            {
                case Protocol.Tcp:
                    NeutronServerFunctions.SocketProtocol(mSender, sendTo, buffer, SendBroadcast(mSender, broadcast), false);
                    break;
                case Protocol.Udp:
                    NeutronServerFunctions.SocketProtocol(mSender, sendTo, buffer, SendBroadcast(mSender, broadcast), true);
                    break;
            }
        }

        public static void Send(this Player mSender, byte[] buffer)
        {
            NeutronServerFunctions.SocketProtocol(mSender, SendTo.Only, buffer, SendBroadcast(mSender, Broadcast.Only), false);
        }

        public static bool IsInChannel(this Player _player)
        {
            return _player.CurrentChannel > -1;
        }

        public static bool IsInRoom(this Player _player)
        {
            return _player.CurrentRoom > -1;
        }

        public static IPEndPoint RemoteEndPoint(this Player socket)
        {
            return (IPEndPoint)socket.tcpClient.Client.RemoteEndPoint;
        }

        public static T DeepClone<T>(this object obj)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, obj);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
            }
        }

        public static object DeepClone(this object type)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, type);
                stream.Position = 0;
                return formatter.Deserialize(stream);
            }
        }

        public static IEnumerable<byte[]> Split(this byte[] bArray, int intBufforLengt)
        {
            int bArrayLenght = bArray.Length;
            byte[] bReturn;

            int i = 0;
            for (; bArrayLenght > (i + 1) * intBufforLengt; i++)
            {
                bReturn = new byte[intBufforLengt];
                Buffer.BlockCopy(bArray, i * intBufforLengt, bReturn, 0, intBufforLengt);
                yield return bReturn;
            }

            int intBufforLeft = bArrayLenght - i * intBufforLengt;
            if (intBufforLeft > 0)
            {
                bReturn = new byte[intBufforLeft];
                Buffer.BlockCopy(bArray, i * intBufforLengt, bReturn, 0, intBufforLeft);
                yield return bReturn;
            }
        }

        public static MethodInfo HasRPC(this NeutronBehaviour mThis, int executeID, out string Error)
        {
            MethodInfo[] infor = mThis.methods;
            if (infor != null)
            {
                for (int i = 0; i < infor.Length; i++)
                {
                    RPC RPC = infor[i].GetCustomAttribute<RPC>();
                    if (RPC != null)
                    {
                        if (RPC.ID == executeID)
                        {
                            ParameterInfo[] pInfor = infor[i].GetParameters();
                            if (pInfor.Length == 3)
                            {
                                if (pInfor[0].ParameterType != typeof(NeutronReader)/* || pInfor[1].ParameterType != typeof(bool)*/)
                                {
                                    Error = $"The scope of the RPC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader)\"";
                                    return null;
                                }
                                else
                                {
                                    Error = null;
                                    return infor[i];
                                }
                            }
                            else
                            {
                                Error = $"The scope of the RPC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader)\"";
                                return null;
                            }
                        }
                    }
                    else continue;
                }
            }
            else NeutronUtils.LoggerError($"Could not find a reference, check if \"base.Awake\" has been implemented in \"{mThis.GetType().Name}\" class.");
            Error = string.Empty;
            return null;
        }

        public static MethodInfo HasAPC(this NeutronBehaviour mThis, int executeID, out string Error)
        {
            MethodInfo[] infor = mThis.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < infor.Length; i++)
            {
                APC APC = infor[i].GetCustomAttribute<APC>();
                if (APC != null)
                {
                    if (APC.ID == executeID)
                    {
                        ParameterInfo[] pInfor = infor[i].GetParameters();
                        if (pInfor.Length == 1)
                        {
                            if (pInfor[0].ParameterType != typeof(NeutronReader))
                            {
                                Error = $"The scope of the APC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader)\"";
                                return null;
                            }
                            else
                            {
                                Error = null;
                                return infor[i];
                            }
                        }
                        else
                        {
                            Error = $"The scope of the APC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader)\"";
                            return null;
                        }
                    }
                }
                else continue;
            }
            Error = string.Empty;
            return null;
        }

        private static Player[] SendBroadcast(Player mPlayer, Broadcast broadcast)
        {
            switch (broadcast)
            {
                case Broadcast.Server:
                    {
                        return Neutron.Server.PlayersBySocket.Values.ToArray();
                    }
                case Broadcast.Channel:
                    {
                        return Neutron.Server.ChannelsById[mPlayer.CurrentChannel].GetPlayers();
                    }
                case Broadcast.Room:
                    {
                        Channel Channel = Neutron.Server.ChannelsById[mPlayer.CurrentChannel];
                        return Channel.GetRoom(mPlayer.CurrentRoom).GetPlayers();
                    }
                case Broadcast.AutoInstantiated:
                    {
                        Channel Channel = Neutron.Server.ChannelsById[mPlayer.CurrentChannel];
                        if (mPlayer.IsInRoom())
                            return Channel.GetRoom(mPlayer.CurrentRoom).GetPlayers().Where(x => x.NeutronView != null).ToArray();
                        else return Channel.GetPlayers().Where(x => x.NeutronView != null).ToArray();
                    }
                case Broadcast.Auto:
                    {
                        if (Neutron.Server.ChannelsById.ContainsKey(mPlayer.CurrentChannel))
                        {
                            Channel Channel = Neutron.Server.ChannelsById[mPlayer.CurrentChannel];
                            if (mPlayer.IsInRoom())
                                return Channel.GetRoom(mPlayer.CurrentRoom).GetPlayers();
                            else return Channel.GetPlayers();
                        }
                        else return Neutron.Server.PlayersBySocket.Values.ToArray();
                    }
                default:
                    return null;
            }
        }
    }
}