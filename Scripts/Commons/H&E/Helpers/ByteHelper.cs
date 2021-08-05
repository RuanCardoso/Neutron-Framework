using NeutronNetwork.Internal.Components;
using Newtonsoft.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NeutronNetwork.Helpers
{
    public static class ByteHelper
    {
        public static NeutronEventWithReturn<object, byte[]> OnCustomSerialization;
        public static NeutronEventWithReturn<byte[], object> OnCustomDeserialization;
        public static NeutronEventWithReturn<byte[], byte[]> OnCustomCompression;
        public static NeutronEventWithReturn<byte[], byte[]> OnCustomDecompression;
        public static byte[] Compress(this byte[] data)
        {
            Compression compression = OthersHelper.GetSettings().GlobalSettings.Compression;
            switch (compression)
            {
                case Compression.Deflate:
                    {
                        using (MemoryStream output = new MemoryStream())
                        {
                            using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Fastest))
                            {
                                dstream.Write(data, 0, data.Length);
                            }
                            return output.ToArray();
                        }
                    }

                case Compression.Gzip:
                    {
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
                case Compression.Custom:
                    return OnCustomCompression?.Invoke(data);
                default:
                    return data;
            }
        }

        public static byte[] Decompress(this byte[] data)
        {
            Compression compression = OthersHelper.GetSettings().GlobalSettings.Compression;
            switch (compression)
            {
                case Compression.Deflate:
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
                case Compression.Gzip:
                    {
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
                case Compression.Custom:
                    return OnCustomDecompression?.Invoke(data);
                default:
                    return data;
            }
        }

        public static byte[] Serialize(this object obj)
        {
            try
            {
                Serialization serializationMode = OthersHelper.GetSettings().GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case Serialization.Json:
                        return NeutronModule.Encoding.GetBytes(JsonConvert.SerializeObject(obj));
                    case Serialization.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream())
                            {
                                formatter.Serialize(mStream, obj);
                                return mStream.ToArray();
                            }
                        }
                    case Serialization.Custom:
                        return OnCustomSerialization?.Invoke(obj);
                    default:
                        return null;
                }
            }
            catch (Exception ex)
            {
                LogHelper.StackTrace(ex);
                return default;
            }
        }

        public static T Deserialize<T>(this byte[] buffer)
        {
            try
            {
                Serialization serialization = OthersHelper.GetSettings().GlobalSettings.Serialization;
                switch (serialization)
                {
                    case Serialization.Json:
                        return JsonConvert.DeserializeObject<T>(NeutronModule.Encoding.GetString(buffer));
                    case Serialization.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream(buffer))
                            {
                                return (T)formatter.Deserialize(mStream);
                            }
                        }
                    case Serialization.Custom:
                        return (T)OnCustomDeserialization?.Invoke(buffer);
                    default:
                        return default;
                }
            }
            catch
            {
                LogHelper.Error("Deserialization exception!");
                return default;
            }
        }

        public static int ReadSize(byte[] headerBuffer)
        {
            switch (OthersHelper.GetConstants().HeaderSize)
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