using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using K4os.Compression.LZ4;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal.Packets;
using Newtonsoft.Json;

namespace NeutronNetwork.Helpers
{
    public static class ByteHelper
    {
        private static Settings Settings => Helper.GetSettings();
        private static NeutronConstantsSettings Constants => Helper.GetConstants();

        public static NeutronEventWithReturn<object, byte[]> OnCustomSerialization;
        public static NeutronEventWithReturn<byte[], object> OnCustomDeserialization;
        public static NeutronEventWithReturn<byte[], byte[]> OnCustomCompression;
        public static NeutronEventWithReturn<byte[], byte[]> OnCustomDecompression;

        public static byte[] Compress(this byte[] data, Internal.Packets.CompressionMode compression)
        {
            switch (compression)
            {
                case Internal.Packets.CompressionMode.Deflate:
                    {
                        using (MemoryStream output = new MemoryStream())
                        {
                            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Fastest))
                            {
                                dstream.Write(data, 0, data.Length);
                            }
                            return output.ToArray(); //* Copy array.
                        }
                    }

                case Internal.Packets.CompressionMode.Gzip:
                    {
                        using (var compressIntoMs = new MemoryStream())
                        {
                            using (var gzs = new BufferedStream(new GZipStream(compressIntoMs,
                                System.IO.Compression.CompressionMode.Compress), 64 * 1024))
                            {
                                gzs.Write(data, 0, data.Length);
                            }
                            return compressIntoMs.ToArray();
                        }
                    }
                case Internal.Packets.CompressionMode.LZ4:
                    return LZ4Pickler.Pickle(data);
                case Internal.Packets.CompressionMode.Custom:
                    return OnCustomCompression?.Invoke(data);
                default:
                    return data;
            }
        }

        public static byte[] Compress(this byte[] data)
        {
            return Compress(data, Settings.GlobalSettings.Compression);
        }

        public static byte[] Decompress(this byte[] data, Internal.Packets.CompressionMode compression)
        {
            switch (compression)
            {
                case Internal.Packets.CompressionMode.Deflate:
                    {
                        using (MemoryStream input = new MemoryStream(data))
                        {
                            using (MemoryStream output = new MemoryStream())
                            {
                                using (DeflateStream dstream = new DeflateStream(input, System.IO.Compression.CompressionMode.Decompress))
                                {
                                    dstream.CopyTo(output);
                                }
                                return output.ToArray();
                            }
                        }
                    }
                case Internal.Packets.CompressionMode.Gzip:
                    {
                        using (var compressedMs = new MemoryStream(data))
                        {
                            using (var decompressedMs = new MemoryStream())
                            {
                                using (var gzs = new BufferedStream(new GZipStream(compressedMs,
                                    System.IO.Compression.CompressionMode.Decompress), 64 * 1024))
                                {
                                    gzs.CopyTo(decompressedMs);
                                }
                                return decompressedMs.ToArray();
                            }
                        }
                    }
                case Internal.Packets.CompressionMode.LZ4:
                    return LZ4Pickler.Unpickle(data);
                case Internal.Packets.CompressionMode.Custom:
                    return OnCustomDecompression?.Invoke(data);
                default:
                    return data;
            }
        }

        public static byte[] Decompress(this byte[] data)
        {
            return Decompress(data, Settings.GlobalSettings.Compression);
        }

        public static byte[] Serialize(this object obj)
        {
            try
            {
                SerializationMode serializationMode = Settings.GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case SerializationMode.Json:
                        return NeutronModule.Encoding.GetBytes(JsonConvert.SerializeObject(obj));
                    case SerializationMode.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream())
                            {
                                formatter.Serialize(mStream, obj);
                                return mStream.ToArray();
                            }
                        }
                    case SerializationMode.Custom:
                        return OnCustomSerialization?.Invoke(obj);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
                return default;
            }
        }

        public static T Deserialize<T>(this byte[] buffer)
        {
            try
            {
                SerializationMode serialization = Settings.GlobalSettings.Serialization;
                switch (serialization)
                {
                    case SerializationMode.Json:
                        return JsonConvert.DeserializeObject<T>(NeutronModule.Encoding.GetString(buffer));
                    case SerializationMode.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream(buffer))
                            {
                                return (T)formatter.Deserialize(mStream);
                            }
                        }
                    case SerializationMode.Custom:
                        return (T)OnCustomDeserialization?.Invoke(buffer);
                    default:
                        return default;
                }
            }
            catch (Exception ex)
            {
                LogHelper.Stacktrace(ex);
                return default;
            }
        }

        public static int ReadSize(byte[] headerBuffer)
        {
            switch (Constants.HeaderSize)
            {
                case HeaderSizeType.Byte:
                    return headerBuffer[0];
                case HeaderSizeType.Short:
                    return BitConverter.ToInt16(headerBuffer, 0);
                case HeaderSizeType.Int:
                    return BitConverter.ToInt32(headerBuffer, 0);
                default:
                    return 0;
            }
        }
    }
}