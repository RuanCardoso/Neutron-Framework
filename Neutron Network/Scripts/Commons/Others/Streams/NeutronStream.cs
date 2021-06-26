using NeutronNetwork.Extensions;
using System;
using System.IO;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronWriter : BinaryWriter, IDisposable
    {
        #region Cached
        private MemoryStream memoryStream;
        #endregion

        public MemoryStream GetStream() => memoryStream;

        public long Pos => memoryStream.Position;
        public long Length => memoryStream.Length;
        public bool Recycle = true;

        public NeutronWriter(bool Recycle = true) : base(new MemoryStream())
        {
            memoryStream = (MemoryStream)base.BaseStream;
            this.Recycle = Recycle;
        }

        public NeutronWriter(MemoryStream newStream, bool Recycle = true) : base(newStream)
        {
            memoryStream = newStream;
            this.Recycle = Recycle;
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
            WriteExactly(buffer);
        }

        public void WritePacket<T>(T packet) where T : Enum
        {
            Write((byte)(object)packet);
        }

        public void WriteFixedLength(int length)
        {
            Write(length);
        }
        /// <summary>
        /// Write Bytes, Serialize.
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="objectToSerialize"></param>
        public void WriteExactly<T>(T objectToSerialize)
        {
            byte[] serializedBytes = objectToSerialize.Serialize();
            WriteExactly(serializedBytes);
        }
        /// <summary>
        /// Write Bytes, Not serialize.
        /// </summary>
        /// <param name="serializedBytes"></param>
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

        public void SetLength(int length)
        {
            GetStream().SetLength(length);
        }

        public void SetBuffer(byte[] buffer, bool resetWriter = false)
        {
            if (resetWriter)
            {
                SetLength(0);
                GetStream().Write(buffer, 0, buffer.Length);
                SetPosition(0);
            }
            else GetStream().Write(buffer, 0, buffer.Length);
        }

        public new void Dispose()
        {
            if (!Recycle)
                base.Dispose();
            else Neutron.PooledNetworkWriters.Push(this);
        }
    }

    public class NeutronReader : BinaryReader, IDisposable
    {
        #region Cached
        private MemoryStream memoryStream;
        #endregion

        public MemoryStream GetStream() => memoryStream;

        public long Pos => memoryStream.Position;
        public long Length => memoryStream.Length;
        public bool Recycle = true;

        public NeutronReader(bool Recycle = true) : base(new MemoryStream())
        {
            memoryStream = (MemoryStream)base.BaseStream;
            this.Recycle = Recycle;
        }

        public NeutronReader(byte[] buffer, bool Recycle = true) : base(new MemoryStream(buffer))
        {
            memoryStream = (MemoryStream)base.BaseStream;
            this.Recycle = Recycle;
        }

        public NeutronReader(byte[] buffer, int index, int count, bool Recycle = true) : base(new MemoryStream(buffer, index, count))
        {
            memoryStream = (MemoryStream)base.BaseStream;
            this.Recycle = Recycle;
        }

        public NeutronReader(MemoryStream newStream, bool Recycle = true) : base(newStream)
        {
            memoryStream = newStream;
            this.Recycle = Recycle;
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
            float[] data = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            return data;
        }

        public T ReadPacket<T>() where T : Enum
        {
            return (T)(object)ReadByte();
        }

        public int ReadFixedLength(int len)
        {
            if (len < sizeof(int)) NeutronLogger.LoggerError($"The first bytes must be at least 4 Bytes, increasing the size of the buffer can solve.: {len}");
            return ReadInt32() + sizeof(int);
        }

        public T ReadExactly<T>()
        {
            return ReadExactly().Deserialize<T>();
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

        public void SetLength(int length)
        {
            GetStream().SetLength(length);
        }

        public void SetBuffer(byte[] buffer, bool resetReader = true)
        {
            if (resetReader)
            {
                SetLength(0);
                GetStream().Write(buffer, 0, buffer.Length);
                SetPosition(0);
            }
            else GetStream().Write(buffer, 0, buffer.Length);
        }

        public new void Dispose()
        {
            if (!Recycle)
                base.Dispose();
            else Neutron.PooledNetworkReaders.Push(this);
        }
    }
}