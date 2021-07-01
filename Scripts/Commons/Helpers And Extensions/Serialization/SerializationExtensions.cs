using NeutronNetwork;
using NeutronNetwork.Internal.Components;
using Newtonsoft.Json;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;

namespace NeutronNetwork.Extensions
{
    public static class SerializationExtensions
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
                        using (NeutronWriter jsonWriter = Neutron.PooledNetworkWriters.Pull())
                        {
                            jsonWriter.SetLength(0);
                            jsonWriter.Write(jsonString);
                            return jsonWriter.ToArray();
                        }
                    case Serialization.BinaryFormatter:
                        BinaryFormatter formatter = new BinaryFormatter();
                        using (MemoryStream mStream = new MemoryStream())
                        {
                            formatter.Serialize(mStream, message);
                            return mStream.ToArray();
                        }
                    default:
                        return null;
                }
            }
            catch (Exception ex) { NeutronLogger.StackTrace(ex); return null; }
        }

        public static T Deserialize<T>(this byte[] message)
        {
            try
            {
                Serialization serializationMode = NeutronConfig.Settings.GlobalSettings.Serialization;
                switch (serializationMode)
                {
                    case Serialization.Json:
                        using (NeutronReader reader = Neutron.PooledNetworkReaders.Pull())
                        {
                            reader.SetBuffer(message);

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
            catch (Exception ex) { NeutronLogger.StackTrace(ex); return default; }
        }
    }
}