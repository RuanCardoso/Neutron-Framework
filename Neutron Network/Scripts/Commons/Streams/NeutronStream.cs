using System;
using System.IO;
using NeutronNetwork.Internal.Extesions;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronWriter : BinaryWriter
    {
        public NeutronWriter() : base(new MemoryStream()) { }
        public NeutronWriter(MemoryStream newStream) : base(newStream) { }
        public MemoryStream GetStream()
        {
            return (MemoryStream)base.BaseStream;
        }

        public void Write(Color writable)
        {
            Write(writable.r);
            Write(writable.g);
            Write(writable.b);
            Write(writable.a);
        }

        public void Write(Vector2 writable)
        {
            Write(writable.x);
            Write(writable.y);
        }

        public void Write(Vector3 writable)
        {
            Write(writable.x);
            Write(writable.y);
            Write(writable.z);
        }

        public void Write(SerializableVector3 writable)
        {
            Write(writable.x);
            Write(writable.y);
            Write(writable.z);
        }

        public void Write(Quaternion writable)
        {
            Write(writable.x);
            Write(writable.y);
            Write(writable.z);
            Write(writable.w);
        }

        public void Write(float[] writable)
        {
            byte[] buffer = new byte[writable.Length * sizeof(float)];
            Buffer.BlockCopy(writable, 0, buffer, 0, buffer.Length);
            buffer = buffer.Compress(NeutronConfig.Settings.GlobalSettings.Compression);
            WriteExactly(buffer);
        }

        public void WritePacket<T>(T packet)
        {
            Write((byte)(object)packet);
        }

        public void WriteFixedLength(int length)
        {
            Write(length);
        }

        public void WriteExactly<T>(T objectToSerialize)
        {
            byte[] serializedBytes = objectToSerialize.Serialize();
            WriteExactly(serializedBytes);
        }

        public void WriteExactly(byte[] serializedBytes)
        {
            WriteFixedLength(serializedBytes.Length);
            Write(serializedBytes);
        }

        public byte[] ToArray()
        {
            return GetStream().ToArray();
        }

        public void SetPosition(int pos)
        {
            GetStream().Position = pos;
        }
    }

    public class NeutronReader : BinaryReader
    {
        public NeutronReader(byte[] buffer) : base(new MemoryStream(buffer)) { }
        public NeutronReader(byte[] buffer, int index, int count) : base(new MemoryStream(buffer, index, count)) { }
        public NeutronReader(MemoryStream newStream) : base(newStream) { }
        public MemoryStream GetStream()
        {
            return (MemoryStream)base.BaseStream;
        }

        public Color ReadColor()
        {
            float r = ReadSingle();
            float g = ReadSingle();
            float b = ReadSingle();
            float a = ReadSingle();
            return new Color(r, g, b, a);
        }

        public Vector2 ReadVector2()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            return new Vector2(x, y);
        }

        public Vector3 ReadVector3()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            return new Vector3(x, y, z);
        }

        public SerializableVector3 ReadSerializableVector3()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            return new SerializableVector3(x, y, z);
        }

        public Quaternion ReadQuaternion()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            float w = ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        public float[] ReadFloatArray()
        {
            byte[] buffer = ReadExactly();
            buffer = buffer.Decompress(NeutronConfig.Settings.GlobalSettings.Compression);
            float[] data = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            return data;
        }

        public T ReadPacket<T>()
        {
            return (T)(object)ReadByte();
        }

        public int ReadFixedLength(int len)
        {
            if (len < sizeof(int)) NeutronUtils.LoggerError($"The first bytes must be at least 4 Bytes, increasing the size of the buffer can solve.: {len}");
            return ReadInt32() + sizeof(int);
        }

        public T ReadExactly<T>()
        {
            return ReadExactly().DeserializeObject<T>();
        }

        public byte[] ReadExactly()
        {
            int len = ReadInt32();
            return ReadBytes(len);
        }

        public byte[] ToArray()
        {
            return GetStream().ToArray();
        }

        public void SetPosition(int pos)
        {
            GetStream().Position = pos;
        }
    }
}