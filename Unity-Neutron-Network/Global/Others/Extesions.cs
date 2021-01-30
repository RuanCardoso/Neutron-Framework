using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal.Server;
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
        public static string Deserialize(this byte[] buffer)
        {
            return Encoding.UTF8.GetString(buffer);
        }

        public static byte[] Serialize(this string message)
        {
            return Encoding.UTF8.GetBytes(message);
        }

        public static byte[] Serialize(this object message)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            using (MemoryStream mStream = new MemoryStream())
            {
                formatter.Serialize(mStream, message);
                return mStream.ToArray();
            }
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
            else if (compressionType == Compression.GZip)
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

        public static byte[] Decompress(this byte[] data, Compression compressionType, int offset, int length)
        {
            if (compressionType == Compression.Deflate)
            {
                using (MemoryStream input = new MemoryStream(data, offset, length))
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
            else if (compressionType == Compression.GZip)
            {
                if (data == null)
                    throw new ArgumentNullException("inputData must be non-null");

                using (var compressedMs = new MemoryStream(data, offset, length))
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
            else
            {
                byte[] nData = new byte[length];
                Buffer.BlockCopy(data, 0, nData, 0, length);
                return nData;
            }
        }

        public static T DeserializeObject<T>(this byte[] message)
        {
            BinaryFormatter formatter = new BinaryFormatter();
            try
            {
                using (MemoryStream mStream = new MemoryStream(message))
                {
                    T obj = (T)formatter.Deserialize(mStream);
                    return obj;
                }
            }
            catch (Exception ex)
            {
                Utils.LoggerError($"Falha ao deserilizar {ex.Message}");
                return default;
            }
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

        public static void Send(this Player mSender, SendTo sendTo, byte[] buffer, Broadcast broadcast, IPEndPoint point, ProtocolType protocolType)
        {
            buffer = buffer.Compress(Neutron.Server.compressionMode);
            switch (protocolType)
            {
                case ProtocolType.Tcp:
                    NeutronSFunc.TCP(mSender.tcpClient, sendTo, buffer, mSender.SendBroadcast(broadcast));
                    break;
                case ProtocolType.Udp:
                    NeutronSFunc.UDP(mSender.tcpClient, sendTo, buffer, mSender.SendBroadcast(broadcast));
                    break;
            }
        }

        public static bool IsInChannel(this Player _player)
        {
            return _player.currentChannel > -1;
        }

        public static bool IsInRoom(this Player _player)
        {
            return _player.currentRoom > -1;
        }

        public static ServerView GetSViewer(this Player _player)
        {
            return Neutron.Server.Players[_player.tcpClient].serverView;
        }

        public static IPEndPoint RemoteEndPoint(this TcpClient socket)
        {
            return (IPEndPoint)socket.Client.RemoteEndPoint;
        }

        private static Player[] SendToRoomAndInstantiated(this Player _player)
        {
            return null;/*Neutron.Server.tcpPlayers.Values.Where(x => x.currentRoom == _player.currentRoom).Where(y => Neutron.Server.playersState.ContainsKey(y.tcpClient)).ToArray();*/
        }

        private static Player[] SendToChannel(this Player channelID)
        {
            return Neutron.Server.Players.Values.Where(x => x.currentChannel == channelID.currentChannel).ToArray();
        }

        private static Player[] SendToRoom(this Player roomID)
        {
            return Neutron.Server.Players.Values.Where(x => x.currentRoom == roomID.currentRoom).ToArray();
        }

        private static Player[] SendToServer()
        {
            return Neutron.Server.Players.Values.ToArray();
        }

        public static void ExecuteOnMainThread(this Action action, Neutron neutronInstance = null, bool executeOnServer = true)
        {
            if (executeOnServer) Utils.Enqueue(action, ref Neutron.Server.monoBehaviourActions);
            else Utils.Enqueue(action, ref neutronInstance.monoBehaviourActions);
        }

        public static T DeepClone<T>(this object list)
        {
            using (MemoryStream stream = new MemoryStream())
            {
                BinaryFormatter formatter = new BinaryFormatter();
                formatter.Serialize(stream, list);
                stream.Position = 0;
                return (T)formatter.Deserialize(stream);
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
            MethodInfo[] infor = mThis.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
            for (int i = 0; i < infor.Length; i++)
            {
                RPC RPC = infor[i].GetCustomAttribute<RPC>();
                if (RPC != null)
                {
                    if (RPC.ID == executeID)
                    {
                        ParameterInfo[] pInfor = infor[i].GetParameters();
                        if (pInfor.Length == 2)
                        {
                            if (pInfor[0].ParameterType != typeof(NeutronReader) || pInfor[1].ParameterType != typeof(bool))
                            {
                                Error = $"The scope of the RPC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader, bool isServer)\"";
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
                            Error = $"The scope of the RPC({executeID}:{mThis.GetType().Name}) is incorrect. Fix to \"void function (NeutronReader reader, bool isServer)\"";
                            return null;
                        }
                    }
                }
                else continue;
            }
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

        private static Player[] SendBroadcast(this Player mPlayer, Broadcast broadcast)
        {
            switch (broadcast)
            {
                case Broadcast.All:
                    return SendToServer();
                case Broadcast.Channel:
                    return mPlayer.SendToChannel();
                case Broadcast.Room:
                    return mPlayer.SendToRoom();
                case Broadcast.Instantiated:
                    return mPlayer.SendToRoomAndInstantiated();
                default:
                    return null;
            }
        }

        public static int GetUniqueID(this GameObject obj)
        {
            return obj.GetInstanceID() ^ new System.Random().Next(0, 1000) ^ DateTime.Now.Millisecond ^ DateTime.Now.Second;
        }
    }
}