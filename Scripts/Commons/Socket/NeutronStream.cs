using NeutronNetwork;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using System;
using System.IO;
using UnityEngine;

public class NeutronStream : IDisposable
{
    public IWriter Writer { get; }
    public IReader Reader { get; }
    public bool IsRecyclable { get; }

    public NeutronStream()
    {
        Writer = new IWriter();
        Reader = new IReader();
    }

    public NeutronStream(bool isRecyclable)
    {
        Writer = new IWriter();
        Reader = new IReader();
        IsRecyclable = isRecyclable;
    }

    public NeutronStream(bool isRecyclable, int capacity)
    {
        Writer = new IWriter(capacity);
        Reader = new IReader(capacity);
        IsRecyclable = isRecyclable;
    }

    public void Dispose()
    {
        Dispose(true);
    }

    private bool _disposing;
    private void Dispose(bool disposing)
    {
        if (!_disposing)
        {
            if (disposing)
            {
                // Managed resources
                if (!IsRecyclable)
                {
                    Writer.Close();
                    Reader.Close();
                    ////////////////////////
                    _disposing = true;
                }
                else
                {
                    Writer.Clear();
                    Reader.Clear();
                    /////////////////////////////////////////
                    Neutron.PooledNetworkStreams.Push(this);
                }
            }
        }
        else
            throw new ObjectDisposedException("Cannot access a disposed object.");
    }

    public class IWriter
    {
        private byte[] _buffer = new byte[8];
        private readonly MemoryStream _stream;
        private bool _isFixedSize;

        public IWriter()
        {
            _stream = new MemoryStream();
        }

        public IWriter(int capacity)
        {
            _stream = new MemoryStream(capacity);
            _isFixedSize = true;
        }

        public void Write(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }

        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }

        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.w);
        }

        public void Write<T>(T[] array, int sizeOf)
        {
            byte[] buffer = new byte[array.Length * sizeOf];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            WriteNextBytes(buffer);
        }

        public void WriteNextBytes(IWriter writer)
        {
            WriteNextBytes(writer.ToArray());
        }

        public void WriteIntWriter(IWriter writer)
        {
            WriteIntExactly(writer.ToArray());
        }

        public void WriteShortWriter(IWriter writer)
        {
            WriteShortExactly(writer.ToArray());
        }

        public void WriteByteWriter(IWriter writer)
        {
            WriteByteExactly(writer.ToArray());
        }

        public void WriteNextBytes(byte[] buffer)
        {
            Write(buffer);
        }

        public void WritePacket<T>(T packet) where T : Enum
        {
            Write((byte)(object)packet);
        }

        public void WriteIntExactly<T>(T obj)
        {
            WriteIntExactly(obj.Serialize());
        }

        public void WriteShortExactly<T>(T obj)
        {
            WriteShortExactly(obj.Serialize());
        }

        public void WriteByteExactly<T>(T obj)
        {
            WriteByteExactly(obj.Serialize());
        }

        public void WriteIntExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > int.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"int\" length: {length}");
            Write(length);
            Write(buffer);
        }

        public void WriteShortExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > short.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"short\" length: {length}");
            Write((short)length);
            Write(buffer);
        }

        public void WriteByteExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > byte.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"byte\" length: {length}");
            Write((byte)length);
            Write(buffer);
        }

        public void Write(byte value)
        {
            _stream.WriteByte(value);
        }

        public void Write(byte[] buffer)
        {
            Write(buffer, 0, buffer.Length);
        }

        public void Write(int value)
        {
            _buffer[0] = (byte)value;
            for (int i = 1; i < sizeof(int); i++)
                _buffer[i] = (byte)(value >> (8 * i));
            Write(_buffer, 0, sizeof(int));
        }

        public void Write(short value)
        {
            _buffer[0] = (byte)value;
            for (int i = 1; i < sizeof(short); i++)
                _buffer[i] = (byte)(value >> (8 * i));
            Write(_buffer, 0, sizeof(short));
        }

        public void Write(string text)
        {
            byte[] encodedText = NeutronModule.Encoding.GetBytes(text);
            Write7BitEncodedInt(encodedText.Length);
            Write(encodedText, 0, encodedText.Length);
        }

        public unsafe void Write(float value)
        {
            uint fUInt = *(uint*)&value;
            _buffer[0] = (byte)fUInt;
            for (int i = 1; i < sizeof(float); i++)
                _buffer[i] = (byte)(fUInt >> (8 * i));
            Write(_buffer, 0, sizeof(float));
        }

        public unsafe void Write(double value)
        {
            ulong fUlong = *(ulong*)&value;
            _buffer[0] = (byte)fUlong;
            for (int i = 1; i < sizeof(double); i++)
                _buffer[i] = (byte)(fUlong >> (8 * i));
            Write(_buffer, 0, sizeof(double));
        }

        public void Write(byte[] buffer, int offset, int size)
        {
            if (_isFixedSize)
            {
                if (GetPosition() < GetCapacity())
                    _stream.Write(buffer, offset, size);
                else
                    throw new Exception("Overflowed the buffer capacity.");
            }
            else
                _stream.Write(buffer, offset, size);
        }

        public void Write7BitEncodedInt(int value)
        {
            // Write out an int 7 bits at a time.  The high bit of the byte,
            // when on, tells reader to continue reading more bytes.
            uint v = (uint)value;   // support negative numbers
            while (v >= 0x80)
            {
                Write((byte)(v | 0x80));
                v >>= 7;
            }
            Write((byte)v);
        }

        public void EndWriteWithFixedCapacity()
        {
            if (_isFixedSize)
            {
                if (GetPosition() == GetCapacity())
                    SetPosition(0);
                else
                    throw new Exception("You have not filled the buffer.");
            }
            else
                throw new Exception("This stream has no defined size.");
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public void SetPosition(int position)
        {
            _stream.Position = position;
        }

        public long GetPosition()
        {
            return _stream.Position;
        }

        public int GetCapacity()
        {
            return _stream.Capacity;
        }

        public void SetCapacity(int size)
        {
            _stream.Capacity = size;
        }

        public void Clear()
        {
            _stream.SetLength(0);
        }

        public void Close()
        {
            _buffer = null;
            _stream.Dispose();
        }
    }
    public class IReader
    {
        private byte[] _buffer = new byte[8];
        private readonly MemoryStream _stream;
        private bool _isFixedSize;

        public IReader()
        {
            _stream = new MemoryStream();
        }

        public IReader(int capacity)
        {
            _stream = new MemoryStream(capacity);
            _isFixedSize = true;
        }

        public Color ReadColor()
        {
            float r = ReadFloat();
            float g = ReadFloat();
            float b = ReadFloat();
            float a = ReadFloat();
            return new Color(r, g, b, a);
        }

        public Vector2 ReadVector2()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            return new Vector2(x, y);
        }

        public Vector3 ReadVector3()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            return new Vector3(x, y, z);
        }

        public Quaternion ReadQuaternion()
        {
            float x = ReadFloat();
            float y = ReadFloat();
            float z = ReadFloat();
            float w = ReadFloat();
            return new Quaternion(x, y, z, w);
        }

        public T[] ReadArray<T>(int sizeOf, int length)
        {
            byte[] buffer = ReadNextBytes(length);
            T[] array = new T[buffer.Length / sizeOf];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        public T ReadPacket<T>() where T : Enum
        {
            return (T)(object)ReadByte();
        }

        /// Ler o tamanho do pacote do cabeçalho do protocolo, com base no HEADER_TYPE.
        public byte[] ReadSize(out int size)
        {
            size = 0;
            switch (OthersHelper.GetConstants().HeaderSize)
            {
                case HeaderSizeType.Byte:
                    {
                        byte[] buffer = ReadByteExactly(out byte sizeOf);
                        size = sizeOf;
                        return buffer;
                    }
                case HeaderSizeType.Short:
                    {
                        byte[] buffer = ReadShortExactly(out short sizeOf);
                        size = sizeOf;
                        return buffer;
                    }
                case HeaderSizeType.Int:
                    {
                        byte[] buffer = ReadIntExactly(out int sizeOf);
                        size = sizeOf;
                        return buffer;
                    }
                default:
                    return default;
            }
        }

        public byte[] ReadNextBytes(int size)
        {
            int bytesRemaining = (int)(size - _stream.Position);
            return ReadBytes(bytesRemaining);
        }

        public T ReadIntExactly<T>()
        {
            return ReadIntExactly().Deserialize<T>();
        }

        public T ReadShortExactly<T>()
        {
            return ReadShortExactly().Deserialize<T>();
        }

        public T ReadByteExactly<T>()
        {
            return ReadByteExactly().Deserialize<T>();
        }

        public byte[] ReadIntExactly()
        {
            return ReadBytes(ReadInt());
        }

        public byte[] ReadShortExactly()
        {
            return ReadBytes(ReadShort());
        }

        public byte[] ReadByteExactly()
        {
            return ReadBytes(ReadByte());
        }

        public byte[] ReadIntExactly(out int size)
        {
            size = ReadInt();
            return ReadBytes(size);
        }

        public byte[] ReadShortExactly(out short size)
        {
            size = ReadShort();
            return ReadBytes(size);
        }

        public byte[] ReadByteExactly(out byte size)
        {
            size = ReadByte();
            return ReadBytes(size);
        }

        public byte ReadByte()
        {
            return (byte)_stream.ReadByte();
        }

        public byte[] ReadBytes(int size)
        {
            byte[] buffer = new byte[size];
            Read(size, buffer);
            return buffer;
        }

        public int ReadInt()
        {
            Read(sizeof(int));
            return (int)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
        }

        public short ReadShort()
        {
            Read(sizeof(short));
            return (short)(_buffer[0] | _buffer[1] << 8);
        }

        public string ReadString()
        {
            int size = Read7BitEncodedInt();
            byte[] textBuffer = new byte[size];
            Read(size, textBuffer);
            return NeutronModule.Encoding.GetString(textBuffer);
        }

        public unsafe float ReadFloat()
        {
            Read(sizeof(float));
            uint fUInt = (uint)(_buffer[0] | _buffer[1] << 8 | _buffer[2] << 16 | _buffer[3] << 24);
            return *((float*)&fUInt);
        }

        public unsafe double ReadDouble()
        {
            Read(sizeof(double));
            uint lo = (uint)(_buffer[0] | _buffer[1] << 8 |
                _buffer[2] << 16 | _buffer[3] << 24);
            uint hi = (uint)(_buffer[4] | _buffer[5] << 8 |
                _buffer[6] << 16 | _buffer[7] << 24);

            ulong fUlong = ((ulong)hi) << 32 | lo;
            return *((double*)&fUlong);
        }

        public void Read(int size, byte[] buffer = null)
        {
            if (buffer == null)
                buffer = _buffer;

            int offset = 0, bytesRead;
            while (offset < size)
            {
                int bytesRemaining = size - offset;
                if ((bytesRead = _stream.Read(buffer, offset, bytesRemaining)) > 0)
                    offset += bytesRead;
                else
                    throw new Exception("The stream is empty or the write and read stream does not match.");
            }
        }

        public int Read7BitEncodedInt()
        {
            // Read out an Int32 7 bits at a time.  The high bit
            // of the byte when on means to continue reading more bytes.
            int count = 0;
            int shift = 0;
            byte b;
            do
            {
                // Check for a corrupted stream.  Read a max of 5 bytes.
                // In a future version, add a DataFormatException.
                if (shift == 5 * 7)  // 5 bytes max per Int32, shift += 7
                    throw new FormatException("Format_Bad7BitInt32");

                // ReadByte handles end of stream cases for us.
                b = ReadByte();
                count |= (b & 0x7F) << shift;
                shift += 7;
            } while ((b & 0x80) != 0);
            return count;
        }

        public void SetBuffer(byte[] buffer)
        {
            _stream.Write(buffer, 0, buffer.Length);
            /////////////////////////////////////////
            SetPosition(0);
        }

        public void EndReadWithFixedCapacity()
        {
            if (_isFixedSize)
                SetPosition(0);
            else
                throw new Exception("This stream has no defined size.");
        }

        public byte[] ToArray()
        {
            return _stream.ToArray();
        }

        public void SetPosition(int position)
        {
            _stream.Position = position;
        }

        public long GetPosition()
        {
            return _stream.Position;
        }

        public int GetCapacity()
        {
            return _stream.Capacity;
        }

        public void SetCapacity(int size)
        {
            _stream.Capacity = size;
        }

        public void Clear()
        {
            _stream.SetLength(0);
        }

        public void Close()
        {
            _buffer = null;
            _stream.Dispose();
        }
    }
}