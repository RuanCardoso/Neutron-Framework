using NeutronNetwork;
using NeutronNetwork.Internal.Extesions;
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
using UnityEngine;

namespace NeutronNetwork.Internal.Extesions
{
    public static class Extesions
    {
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

        public static bool IsConnected(this TcpClient socket)
        {
            try
            {
                return !(socket.Client.Poll(1, SelectMode.SelectRead) && socket.Available == 0);
            }
            catch (SocketException) { return false; }
        }

        public static void Send(this Player mSender, NeutronWriter writer, SendTo sendTo, Broadcast broadcast, Protocol protocolType)
        {
            SocketHelper.Redirect(mSender, protocolType, sendTo, writer.ToArray(), Ext.Broadcast(mSender, broadcast));
        }

        public static void Send(this Player mSender, NeutronWriter writer, Handle handle)
        {
            SocketHelper.Redirect(mSender, handle.protocol, handle.sendTo, writer.ToArray(), Ext.Broadcast(mSender, handle.broadcast));
        }

        public static void Send(this Player mSender, NeutronWriter writer)
        {
            SocketHelper.Redirect(mSender, Protocol.Tcp, SendTo.Only, writer.ToArray(), Ext.Broadcast(mSender, global::Broadcast.Only));
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

        public static IList<T> Clone<T>(this IList<T> listToClone) where T : ICloneable
        {
            return listToClone.Select(item => (T)item.Clone()).ToList();
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
#if !UNITY_2019_2_OR_NEWER
        public static bool TryGetComponent<T>(this GameObject monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }

        public static bool TryGetComponent<T>(this Transform monoBehaviour, out T component)
        {
            component = monoBehaviour.GetComponent<T>();
            if (component != null)
                return (component.ToString() != null && component.ToString() != "null");
            else return false;
        }
#endif
    }
}

public class Ext
{
    public static Player[] Broadcast(Player mPlayer, Broadcast broadcast)
    {
        INeutronMatchmaking matchmaking = MatchmakingHelper.Matchmaking(mPlayer);
        switch (broadcast)
        {
            case global::Broadcast.Server:
                return Neutron.Server.PlayersBySocket.Values.ToArray();
            case global::Broadcast.Channel:
                {
                    if (Neutron.Server.ChannelsById.TryGetValue(mPlayer.CurrentChannel, out Channel l_Channel))
                        return l_Channel.GetPlayers();
                    else return null;
                }
            case global::Broadcast.Room:
                {
                    if (Neutron.Server.ChannelsById.TryGetValue(mPlayer.CurrentChannel, out Channel l_Channel))
                    {
                        Room l_Room = l_Channel.GetRoom(mPlayer.CurrentRoom);
                        if (l_Room != null)
                            return l_Room.GetPlayers();
                        else return null;
                    }
                    else return null;
                }
            case global::Broadcast.Auto:
                {
                    if (matchmaking != null)
                        return matchmaking.GetPlayers();
                    else return Neutron.Server.PlayersBySocket.Values.ToArray();
                }
            default:
                return null;
        }
    }
}