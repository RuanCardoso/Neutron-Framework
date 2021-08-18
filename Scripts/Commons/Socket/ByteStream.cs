using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Components;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using System;
using System.IO;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronWriter : BinaryWriter, INeutronWriter, IDisposable
    {
        public static NeutronWriter Empty = new NeutronWriter();

        #region Fields
        private readonly MemoryStream _memoryStream;
        private readonly bool _recycle = true;
        #endregion

        #region Properties
        public MemoryStream Stream => _memoryStream;
        public long Pos => _memoryStream.Position;
        public long Length => _memoryStream.Length;
        #endregion

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para escrever bytes.
        /// </summary>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronWriter(bool recycle = true) : base(new MemoryStream(), NeutronModule.Encoding)
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para escrever bytes.
        /// </summary>
        /// <param name="stream">* Define um stream personalizado.</param>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronWriter(MemoryStream stream, bool recycle = true) : base(stream, NeutronModule.Encoding)
        {
            _memoryStream = stream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Color"></see>.
        /// </summary>
        public void Write(Color color)
        {
            Write(color.r);
            Write(color.g);
            Write(color.b);
            Write(color.a);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Vector2"></see>.
        /// </summary>
        public void Write(Vector2 vector)
        {
            Write(vector.x);
            Write(vector.y);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Vector3"></see>.
        /// </summary>
        public void Write(Vector3 vector)
        {
            Write(vector.x);
            Write(vector.y);
            Write(vector.z);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Quaternion"></see>.
        /// </summary>
        public void Write(Quaternion quaternion)
        {
            Write(quaternion.x);
            Write(quaternion.y);
            Write(quaternion.z);
            Write(quaternion.w);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="float[]"></see>.
        /// </summary>
        public void Write(float[] array)
        {
            byte[] buffer = new byte[array.Length * sizeof(float)];
            Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
            WriteIntExactly(buffer);
        }

        /// Escreve o tamanho do pacote no cabeçalho do protocolo, com base no HEADER_TYPE.
        public void WriteSize(byte[] packetBuffer)
        {
            switch (OthersHelper.GetConstants().HeaderSize)
            {
                case HeaderSizeType.Byte:
                    WriteByteExactly(packetBuffer);
                    break;
                case HeaderSizeType.Short:
                    WriteShortExactly(packetBuffer);
                    break;
                case HeaderSizeType.Int:
                    WriteIntExactly(packetBuffer);
                    break;
            }
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="NeutronWriter"></see>.
        /// </summary>
        public void WriteNextBytes(NeutronWriter writer)
        {
            WriteNextBytes(writer.ToArray());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="byte[]"></see>.
        /// </summary>
        public void WriteNextBytes(byte[] buffer)
        {
            Write(buffer);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="NeutronWriter"></see>.
        /// </summary>
        public void WriteIntWriter(NeutronWriter writer)
        {
            WriteIntExactly(writer.ToArray());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="NeutronWriter"></see>.
        /// </summary>
        public void WriteShortWriter(NeutronWriter writer)
        {
            WriteShortExactly(writer.ToArray());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="NeutronWriter"></see>.
        /// </summary>
        public void WriteByteWriter(NeutronWriter writer)
        {
            WriteByteExactly(writer.ToArray());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Enum"></see>.
        /// </summary>
        public void WritePacket(byte packet)
        {
            Write(packet);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="T"></see>.<br/>
        ///* A instância <see cref="T"/> é serializada utilizando a serialização definida nas configurações.
        /// </summary>
        public void WriteIntExactly<T>(T obj)
        {
            WriteIntExactly(obj.Serialize());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="T"></see>.<br/>
        ///* A instância <see cref="T"/> é serializada utilizando a serialização definida nas configurações.
        /// </summary>
        public void WriteShortExactly<T>(T obj)
        {
            WriteShortExactly(obj.Serialize());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="T"></see>.<br/>
        ///* A instância <see cref="T"/> é serializada utilizando a serialização definida nas configurações.
        /// </summary>
        public void WriteByteExactly<T>(T obj)
        {
            WriteByteExactly(obj.Serialize());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="byte[]"></see>.<br/>
        /// </summary>
        public void WriteIntExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > int.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"int\" length: {length}");
            Write(length);
            Write(buffer);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="byte[]"></see>.<br/>
        /// </summary>
        public void WriteShortExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > short.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"short\" length: {length}");
            Write((short)length);
            Write(buffer);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="byte[]"></see>.<br/>
        /// </summary>
        public void WriteByteExactly(byte[] buffer)
        {
            int length = buffer.Length;
            if (length > byte.MaxValue)
                LogHelper.Error($"Header size overflow, size is greater than \"byte\" length: {length}");
            Write((byte)length);
            Write(buffer);
        }

        /// <summary>
        ///* Retorna o fluxo de bytes.
        /// </summary>
        public byte[] ToArray()
        {
            return Stream.ToArray();
        }

        /// <summary>
        ///* Define uma posição no fluxo de bytes.
        /// </summary>
        public void SetPosition(int pos)
        {
            Stream.Position = pos;
        }

        /// <summary>
        ///* Define o tamanho do fluxo de bytes.
        /// </summary>
        public void SetLength(int length)
        {
            Stream.SetLength(length);
        }

        /// <summary>
        ///* Escreve uma matriz do tipo <see cref="byte[]"/> no fluxo de bytes.
        /// </summary>
        public void SetBuffer(byte[] buffer)
        {
            Stream.Write(buffer, 0, buffer.Length);
        }

        /// <summary>
        ///* Descarta a instância, se recycle é true, recicla a instância.
        /// </summary>
        public new void Dispose()
        {
            if (!_recycle)
                base.Dispose();
            else
            {
                SetLength(0);
                Neutron.PooledNetworkWriters.Push(this);
            }
        }

        public void EndWriteWithFixedCapacity()
        {
            throw new NotImplementedException();
        }

        public void Write<T>(T[] array, Int32 sizeOf)
        {
            throw new NotImplementedException();
        }

        void INeutronWriter.Write7BitEncodedInt(Int32 value)
        {
            throw new NotImplementedException();
        }

        public void WriteByteWriter(NeutronStream.IWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteIntWriter(NeutronStream.IWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteNextBytes(NeutronStream.IWriter writer)
        {
            throw new NotImplementedException();
        }

        public void WriteShortWriter(NeutronStream.IWriter writer)
        {
            throw new NotImplementedException();
        }

        public Byte[] GetBuffer()
        {
            throw new NotImplementedException();
        }

        public Int64 GetPosition()
        {
            throw new NotImplementedException();
        }

        public Int32 GetCapacity()
        {
            throw new NotImplementedException();
        }

        public void SetCapacity(Int32 size)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Boolean IsFixedSize()
        {
            throw new NotImplementedException();
        }

        public void EndWrite()
        {
            throw new NotImplementedException();
        }
    }

    public class NeutronReader : BinaryReader, INeutronReader, IDisposable
    {
        #region Fields
        private readonly MemoryStream _memoryStream;
        private readonly bool _recycle = true;
        #endregion

        #region Properties
        public MemoryStream Stream => _memoryStream;
        public long Pos => _memoryStream.Position;
        public long Length => _memoryStream.Length;
        #endregion

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronReader(bool recycle = true) : base(new MemoryStream(), NeutronModule.Encoding)
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronReader(MemoryStream stream, bool recycle = true) : base(stream, NeutronModule.Encoding)
        {
            _memoryStream = stream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        public NeutronReader(byte[] buffer) : base(new MemoryStream(buffer), NeutronModule.Encoding)
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = false;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        public NeutronReader(byte[] buffer, int index, int count) : base(new MemoryStream(buffer, index, count), NeutronModule.Encoding)
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = false;
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Vector3"></see> do fluxo de bytes.
        /// </summary>
        public Color ReadColor()
        {
            float r = ReadSingle();
            float g = ReadSingle();
            float b = ReadSingle();
            float a = ReadSingle();
            return new Color(r, g, b, a);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Vector2"></see> do fluxo de bytes.
        /// </summary>
        public Vector2 ReadVector2()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            return new Vector2(x, y);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Vector3"></see> do fluxo de bytes.
        /// </summary>
        public Vector3 ReadVector3()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            return new Vector3(x, y, z);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Quaternion"></see> do fluxo de bytes.
        /// </summary>
        public Quaternion ReadQuaternion()
        {
            float x = ReadSingle();
            float y = ReadSingle();
            float z = ReadSingle();
            float w = ReadSingle();
            return new Quaternion(x, y, z, w);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="float[]"></see> do fluxo de bytes.
        /// </summary>
        public float[] ReadFloatArray()
        {
            byte[] buffer = ReadIntExactly();
            float[] array = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
            return array;
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Enum"></see> do fluxo de bytes.
        /// </summary>
        public byte ReadPacket()
        {
            return ReadByte();
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

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadNextBytes(int size)
        {
            int bytesRemaining = (int)(size - Pos);
            return ReadBytes(bytesRemaining);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="T"></see> do fluxo de bytes e deserializa.
        /// </summary>
        public T ReadIntExactly<T>()
        {
            return ReadIntExactly().Deserialize<T>();
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="T"></see> do fluxo de bytes e deserializa.
        /// </summary>
        public T ReadShortExactly<T>()
        {
            return ReadShortExactly().Deserialize<T>();
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="T"></see> do fluxo de bytes e deserializa.
        /// </summary>
        public T ReadByteExactly<T>()
        {
            return ReadByteExactly().Deserialize<T>();
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadIntExactly()
        {
            return ReadBytes(ReadInt32());
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadShortExactly()
        {
            return ReadBytes(ReadInt16());
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadByteExactly()
        {
            return ReadBytes(ReadByte());
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadIntExactly(out int size)
        {
            size = ReadInt32();
            return ReadBytes(size);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadShortExactly(out short size)
        {
            size = ReadInt16();
            return ReadBytes(size);
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadByteExactly(out byte size)
        {
            size = ReadByte();
            return ReadBytes(size);
        }

        /// <summary>
        ///* Retorna o fluxo de bytes.
        /// </summary>
        public byte[] ToArray()
        {
            return Stream.ToArray();
        }

        /// <summary>
        ///* Define uma posição no fluxo de bytes.
        /// </summary>
        public void SetPosition(int pos)
        {
            Stream.Position = pos;
        }

        /// <summary>
        ///* Define o tamanho do fluxo de bytes.
        /// </summary>
        public void SetLength(int length)
        {
            Stream.SetLength(length);
        }

        /// <summary>
        ///* Escreve uma matriz do tipo <see cref="byte[]"/> no fluxo de bytes.
        /// </summary>
        public void SetBuffer(byte[] buffer)
        {
            Stream.Write(buffer, 0, buffer.Length);
            SetPosition(0);
        }

        /// <summary>
        ///* Descarta a instância, se "recycle" é true, recicla a instância para o pool.
        /// </summary>
        public new void Dispose()
        {
            if (!_recycle)
                base.Dispose();
            else
            {
                SetLength(0);
                Neutron.PooledNetworkReaders.Push(this);
            }
        }

        public void EndReadWithFixedCapacity()
        {
            throw new NotImplementedException();
        }

        public void Read(Int32 size, Byte[] buffer = null)
        {
            throw new NotImplementedException();
        }

        Int32 INeutronReader.Read7BitEncodedInt()
        {
            throw new NotImplementedException();
        }

        public T[] ReadArray<T>(Int32 sizeOf, Int32 length)
        {
            throw new NotImplementedException();
        }

        public Single ReadFloat()
        {
            throw new NotImplementedException();
        }

        public Int32 ReadInt()
        {
            throw new NotImplementedException();
        }

        public Int16 ReadShort()
        {
            throw new NotImplementedException();
        }

        public Byte[] GetBuffer()
        {
            throw new NotImplementedException();
        }

        public Int64 GetPosition()
        {
            throw new NotImplementedException();
        }

        public Int32 GetCapacity()
        {
            throw new NotImplementedException();
        }

        public void SetCapacity(Int32 size)
        {
            throw new NotImplementedException();
        }

        public void Clear()
        {
            throw new NotImplementedException();
        }

        public Boolean IsFixedSize()
        {
            throw new NotImplementedException();
        }

        public void EndRead()
        {
            throw new NotImplementedException();
        }
    }
}