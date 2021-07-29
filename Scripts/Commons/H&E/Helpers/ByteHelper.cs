using NeutronNetwork.Internal.Components;
using NeutronNetwork.Json;
using System;
using System.IO;
using System.IO.Compression;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace NeutronNetwork.Helpers
{
    public static class ByteHelper
    {
        public static byte[] Compress(this byte[] data)
        {
            Compression compression = NeutronMain.Settings.GlobalSettings.Compression;
            if (compression == Compression.Deflate)
            {
                using (MemoryStream output = new MemoryStream())
                {
                    using (DeflateStream dstream = new DeflateStream(output, CompressionLevel.Optimal))
                    {
                        dstream.Write(data, 0, data.Length);
                    }
                    return output.ToArray();
                }
            }
            else if (compression == Compression.Gzip)
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

        public static byte[] Decompress(this byte[] data)
        {
            Compression compression = NeutronMain.Settings.GlobalSettings.Compression;
            if (compression == Compression.Deflate)
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
            else if (compression == Compression.Gzip)
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

        public static byte[] Serialize(this object obj)
        {
            try
            {
                Serialization serializationMode = NeutronMain.Settings.GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case Serialization.Json:
                        {
                            return Encoding.UTF8.GetBytes(JsonConvert.SerializeObject(obj));
                        }
                    case Serialization.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream())
                            {
                                formatter.Serialize(mStream, obj);
                                return mStream.ToArray();
                            }
                        }
                    default:
                        return null;
                }
            }
            catch (Exception ex) { LogHelper.StackTrace(ex); return null; }
        }

        public static T Deserialize<T>(this byte[] buffer)
        {
            try
            {
                Serialization serialization = NeutronMain.Settings.GlobalSettings.Serialization;
                switch (serialization)
                {
                    case Serialization.Json:
                        {
                            return JsonConvert.DeserializeObject<T>(Encoding.UTF8.GetString(buffer));
                        }
                    case Serialization.Binary:
                        {
                            BinaryFormatter formatter = new BinaryFormatter();
                            using (MemoryStream mStream = new MemoryStream(buffer))
                            {
                                return (T)formatter.Deserialize(mStream);
                            }
                        }
                    default:
                        return default;
                }
            }
            catch (Exception ex) { LogHelper.StackTrace(ex); return default; }
        }
    }
}