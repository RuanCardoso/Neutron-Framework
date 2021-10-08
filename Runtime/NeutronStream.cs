using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using System;
using System.IO;
using System.Runtime.InteropServices;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronStream : IDisposable
    {
        public static NeutronStream Empty = new NeutronStream();
        /// <summary>
        ///* Escritor utilizado para escrever o pacote.
        /// </summary>
        public IWriter Writer {
            get;
        }

        //* Escritor utilizado para escrever o cabeçalho.
#pragma warning disable IDE1006
        public IWriter hWriter {
#pragma warning restore IDE1006
            get;
        }

        /// <summary>
        ///* Leitor utilizado para ler o pacote.
        /// </summary>
        public IReader Reader {
            get;
        }

        /// <summary>
        ///* Retorna se o stream é reciclável.
        /// </summary>
        public bool IsRecyclable {
            get;
        }

        /// <summary>
        ///* Retorna se o stream possui um tamanho fixo.
        /// </summary>
        public bool IsFixedSize {
            get;
        }

        public NeutronStream()
        {
            Writer = new IWriter();
            Reader = new IReader();
            hWriter = new IWriter();
        }

        /// <param name="capacity">* Define a capacidade do stream.</param>
        public NeutronStream(int capacity)
        {
            Writer = new IWriter(capacity);
            Reader = new IReader(capacity);
            hWriter = new IWriter(capacity);
            IsFixedSize = capacity > 0;
        }

        /// <param name="capacity">* Define se o stream é reciclável.</param>
        public NeutronStream(bool isRecyclable)
        {
            Writer = new IWriter();
            Reader = new IReader();
            hWriter = new IWriter();
            IsRecyclable = isRecyclable;
        }

        /// <param name="capacity">* Define se o stream é reciclável.</param>
        /// <param name="capacity">* Define a capacidade do stream.</param>
        public NeutronStream(bool isRecyclable, int capacity)
        {
            Writer = new IWriter(capacity);
            Reader = new IReader(capacity);
            hWriter = new IWriter(capacity);
            IsRecyclable = isRecyclable;
            IsFixedSize = capacity > 0;
        }

        public void Dispose() => Dispose(true);

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
                        hWriter.Close();
                        _disposing = true;
                    }
                    else
                    {
                        Writer.Clear();
                        Reader.Clear();
                        hWriter.Clear();
                        Neutron.PooledNetworkStreams.Push(this);
                    }
                }
            }
            else
                throw new ObjectDisposedException("Cannot access a disposed object.");
        }

        public class IWriter : INeutronWriter
        {
            private byte[] _buffer = new byte[8]; //* Suporta até double(8 bytes).
            private readonly MemoryStream _stream;
            private readonly bool _isFixedSize;
            private readonly int _fixedCapacity;

            public IWriter()
            {
                _stream = new MemoryStream();
            }

            public IWriter(int capacity)
            {
                _stream = new MemoryStream(capacity);
                _fixedCapacity = capacity;
                _isFixedSize = _fixedCapacity > 0;
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Color"/> com tamanho fixo de 16 Bytes(float4).
            /// </summary>
            public void Write(Color color)
            {
                Write(color.r);
                Write(color.g);
                Write(color.b);
                Write(color.a);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Vector2"/> com tamanho fixo de 8 Bytes(float2).
            /// </summary>
            public void Write(Vector2 vector)
            {
                Write(vector.x);
                Write(vector.y);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Vector3"/> com tamanho fixo de 12 Bytes(float3).
            /// </summary>
            public void Write(Vector3 vector)
            {
                Write(vector.x);
                Write(vector.y);
                Write(vector.z);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Quaternion"/> com tamanho fixo de 16 Bytes(float4).
            /// </summary>
            public void Write(Quaternion quaternion)
            {
                Write(quaternion.x);
                Write(quaternion.y);
                Write(quaternion.z);
                Write(quaternion.w);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Quaternion"/> compactado de 16 bytes a 7 Bytes.
            /// </summary>
            public void WriteCompressed(Quaternion quaternion, float FLOAT_PRECISION_MULT) // Copyright (c) 2016 StagPoint Software
            {
                short a, b, c;

                var maxIndex = (byte)0;
                var maxValue = float.MinValue;
                var sign = 1f;

                for (int i = 0; i < 4; i++)
                {
                    var element = quaternion[i];
                    var abs = Mathf.Abs(quaternion[i]);
                    if (abs > maxValue)
                    {
                        sign = (element < 0) ? -1 : 1;

                        maxIndex = (byte)i;
                        maxValue = abs;
                    }
                }

                if (Mathf.Approximately(maxValue, 1f))
                {
                    Write((byte)(maxIndex + 4));
                    return;
                }

                switch (maxIndex)
                {
                    case 0:
                        a = (short)(quaternion.y * sign * FLOAT_PRECISION_MULT);
                        b = (short)(quaternion.z * sign * FLOAT_PRECISION_MULT);
                        c = (short)(quaternion.w * sign * FLOAT_PRECISION_MULT);
                        break;
                    case 1:
                        a = (short)(quaternion.x * sign * FLOAT_PRECISION_MULT);
                        b = (short)(quaternion.z * sign * FLOAT_PRECISION_MULT);
                        c = (short)(quaternion.w * sign * FLOAT_PRECISION_MULT);
                        break;
                    case 2:
                        a = (short)(quaternion.x * sign * FLOAT_PRECISION_MULT);
                        b = (short)(quaternion.y * sign * FLOAT_PRECISION_MULT);
                        c = (short)(quaternion.w * sign * FLOAT_PRECISION_MULT);
                        break;
                    default:
                        a = (short)(quaternion.x * sign * FLOAT_PRECISION_MULT);
                        b = (short)(quaternion.y * sign * FLOAT_PRECISION_MULT);
                        c = (short)(quaternion.z * sign * FLOAT_PRECISION_MULT);
                        break;
                }

                Write(maxIndex);
                Write(a);
                Write(b);
                Write(c);
            }
            public void WriteCompressed(Vector3 vector3)
            {
                //float x = 123.45;
                //ushort fixedIntValue = (ushort)(value * 256);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Array"/> com tamanho fixo de <see cref="Marshal.SizeOf(typeof(T))]"/>.<br/>
            ///* Suporta apenas dados que são tipos de valores e primitivos(<see cref="bool"/>, <see cref="byte"/>, <see cref="int"/>, <see cref="float"/>, <see cref="double"/>, <see cref="char"/>, <see cref="short"/>)...etc
            /// </summary>
            public void Write<T>(T[] array)
            {
                byte[] buffer = new byte[array.Length * Marshal.SizeOf(typeof(T))];
                Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
                WriteWithShort(buffer);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="Array"/> com tamanho fixo de <see cref="Marshal.SizeOf(typeof(T))]"/>.<br/>
            ///* O stream só pode chamar este metódo uma vez por chamada e deve ser escrito na última posição.<br/>
            ///* Suporta apenas dados que são tipos de valores e primitivos(<see cref="bool"/>, <see cref="byte"/>, <see cref="int"/>, <see cref="float"/>, <see cref="double"/>, <see cref="char"/>, <see cref="short"/>)...etc<br/>
            ///* Economiza largura de banda em relação ao seu metódo concorrente <see cref="Write{T}(T[])"/> pois não carrega um cabeçalho com o tamanho da matriz.
            /// </summary>
            public void WriteNext<T>(T[] array)
            {
                byte[] buffer = new byte[array.Length * Marshal.SizeOf(typeof(T))];
                Buffer.BlockCopy(array, 0, buffer, 0, buffer.Length);
                WriteNext(buffer);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[] com tamanho fixo ou não-fixo.<br/>
            ///* O tamanho da matriz será armazenado no cabeçalho com o tipo definido nas configurações.
            /// </summary>
            public void WriteByteArrayWithAutoSize(byte[] buffer)
            {
                switch (Helper.GetConstants().HeaderSize)
                {
                    case HeaderSizeType.Byte:
                        WriteWithByte(buffer);
                        break;
                    case HeaderSizeType.Short:
                        WriteWithShort(buffer);
                        break;
                    case HeaderSizeType.Int:
                        WriteWithInteger(buffer);
                        break;
                }
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="IWriter"/>, deve ser escrito na última posição pois não registra o tamanho no cabeçalho, economizando largura de banda.<br/>
            ///* O stream só pode chamar este metódo uma vez por chamada.
            /// </summary>
            public void WriteNext(IWriter writer)
            {
                WriteNext(writer.ToArray());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="IWriter"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="int"/> de tamanho fixo de 4 Bytes.
            /// </summary>
            public void WriteWithInteger(IWriter writer)
            {
                WriteWithInteger(writer.ToArray());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="IWriter"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="short"/> de tamanho fixo de 2 Bytes.
            /// </summary>
            public void WriteWithShort(IWriter writer)
            {
                WriteWithShort(writer.ToArray());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="IWriter"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="byte"/> de tamanho fixo de 1 Byte.
            /// </summary>
            public void WriteWithByte(IWriter writer)
            {
                WriteWithByte(writer.ToArray());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[], deve ser escrito na última posição pois não registra o tamanho no cabeçalho, economizando largura de banda.<br/>
            ///* O stream só pode chamar este metódo uma vez por chamada.
            /// </summary>
            public void WriteNext(byte[] buffer)
            {
                Write(buffer);
            }

            public void WritePacket(byte packet)
            {
                Write(packet);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="{T}"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="int"/> de tamanho fixo de 4 Bytes.<br/>
            ///* Serializa os dados com a serialização definida nas configurações.
            /// </summary>
            public void WriteWithInteger<T>(T obj)
            {
                WriteWithInteger(obj.Serialize());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="{T}"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="short"/> de tamanho fixo de 2 Bytes.<br/>
            ///* Serializa os dados com a serialização definida nas configurações.
            /// </summary>
            public void WriteWithShort<T>(T obj)
            {
                WriteWithShort(obj.Serialize());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="{T}"/>, o tamanho no cabeçalho é registrado com o tipo <see cref="byte"/> de tamanho fixo de 1 Byte.<br/>
            ///* Serializa os dados com a serialização definida nas configurações.
            /// </summary>
            public void WriteWithByte<T>(T obj)
            {
                WriteWithByte(obj.Serialize());
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[], o tamanho no cabeçalho é registrado com o tipo <see cref="int"/> de tamanho fixo de 4 Bytes.
            /// </summary>
            public void WriteWithInteger(byte[] buffer)
            {
                int length = buffer.Length;
                if (length > int.MaxValue)
                    throw new Exception($"Header size overflow, size is greater than \"int\" length: {length}");
                Write(length);
                Write(buffer);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[], o tamanho no cabeçalho é registrado com o tipo <see cref="short"/> de tamanho fixo de 2 Bytes.
            /// </summary>
            public void WriteWithShort(byte[] buffer)
            {
                int length = buffer.Length;
                if (length > short.MaxValue)
                    throw new Exception($"Header size overflow, size is greater than \"short\" length: {length}");
                Write((short)length);
                Write(buffer);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[], o tamanho no cabeçalho é registrado com o tipo <see cref="byte"/> de tamanho fixo de 1 Byte.
            /// </summary>
            public void WriteWithByte(byte[] buffer)
            {
                int length = buffer.Length;
                if (length > byte.MaxValue)
                    throw new Exception($"Header size overflow, size is greater than \"byte\" length: {length}");
                Write((byte)length);
                Write(buffer);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/> com tamanho fixo de 1 Byte(byte).
            /// </summary>
            public void Write(byte value)
            {
                _stream.WriteByte(value);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="bool"/> com tamanho fixo de 1 Byte(byte).
            /// </summary>
            public void Write(bool value)
            {
                _stream.WriteByte((byte)(value ? 1 : 0));
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[] com tamanho fixo e não fixo, não registra cabeçalho.
            /// </summary>
            public void Write(byte[] buffer)
            {
                Write(buffer, 0, buffer.Length);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="int"/> com tamanho fixo de 4 Bytes(bytes).
            /// </summary>
            public void Write(int value)
            {
                _buffer[0] = (byte)value;
                for (int i = 1; i < sizeof(int); i++)
                    _buffer[i] = (byte)(value >> (8 * i));
                Write(_buffer, 0, sizeof(int));
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="short"/> com tamanho fixo de 2 Bytes(bytes).
            /// </summary>
            public void Write(short value)
            {
                _buffer[0] = (byte)value;
                for (int i = 1; i < sizeof(short); i++)
                    _buffer[i] = (byte)(value >> (8 * i));
                Write(_buffer, 0, sizeof(short));
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="ushort"/> com tamanho fixo de 2 Bytes(bytes).
            /// </summary>
            public void Write(ushort value)
            {
                _buffer[0] = (byte)value;
                for (int i = 1; i < sizeof(ushort); i++)
                    _buffer[i] = (byte)(value >> (8 * i));
                Write(_buffer, 0, sizeof(ushort));
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="string"/> com tamanho dinâmico.
            /// </summary>
            public void Write(string text)
            {
                byte[] encodedText = NeutronModule.Encoding.GetBytes(text);
                Write7BitEncodedInt(encodedText.Length);
                Write(encodedText, 0, encodedText.Length);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="float"/> com tamanho fixo de 4 Bytes(bytes).
            /// </summary>
            public unsafe void Write(float value)
            {
                uint fUInt = *(uint*)&value;
                _buffer[0] = (byte)fUInt;
                for (int i = 1; i < sizeof(float); i++)
                    _buffer[i] = (byte)(fUInt >> (8 * i));
                Write(_buffer, 0, sizeof(float));
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="double"/> com tamanho fixo de 8 Bytes(bytes).
            /// </summary>
            public unsafe void Write(double value)
            {
                ulong fUlong = *(ulong*)&value;
                _buffer[0] = (byte)fUlong;
                for (int i = 1; i < sizeof(double); i++)
                    _buffer[i] = (byte)(fUlong >> (8 * i));
                Write(_buffer, 0, sizeof(double));
            }

            /// <summary>
            ///* Escreve no buffer o tipo de estrutura(struct) do tipo <see cref="{T}"/> com tamanho fixo de <see cref="Marshal.SizeOf(typeof(T))]"/>
            /// </summary>
            public unsafe void Marshalling_Write<T>(T structure) where T : struct
            {
                byte[] byteArray = new byte[Marshal.SizeOf(structure)];
                fixed (byte* byteArrayPtr = byteArray)
                    Marshal.StructureToPtr(structure, (IntPtr)byteArrayPtr, true);
                WriteWithShort(byteArray);
            }

            /// <summary>
            ///* Escreve no buffer o tipo de estrutura(struct) do tipo <see cref="{T}"/> com tamanho fixo de <see cref="Marshal.SizeOf(typeof(T))]"/><br/>
            ///* O stream só pode chamar este metódo uma vez por chamada e deve ser escrito na última posição pois não registra cabeçalho.
            /// </summary>
            public unsafe void Marshalling_WriteNext<T>(T structure) where T : struct
            {
                byte[] byteArray = new byte[Marshal.SizeOf(structure)];
                fixed (byte* byteArrayPtr = byteArray)
                    Marshal.StructureToPtr(structure, (IntPtr)byteArrayPtr, true);
                WriteNext(byteArray);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="byte"/>[] com tamanho fixo e não fixo, não registra cabeçalho.
            /// </summary>
            public void Write(byte[] buffer, int offset, int size)
            {
                if (_isFixedSize)
                {
                    if (GetPosition() < _fixedCapacity)
                        _stream.Write(buffer, offset, size);
                    else
                        throw new Exception("Overflowed the buffer capacity.");
                }
                else
                    _stream.Write(buffer, offset, size);
            }

            /// <summary>
            ///* Escreve no buffer o tipo <see cref="int"/> com tamanho fixo de 7 bits(compactado).
            /// </summary>
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

            /// <summary>
            ///* Finaliza a escrita do buffer com tamanho fixo.
            /// </summary>
            public void EndWriteWithFixedCapacity()
            {
                if (_isFixedSize)
                {
                    if (GetPosition() == GetCapacity())
                        SetPosition(0);
                    else
                        throw new Exception("You did not fill the buffer or overflowed.");
                }
                else
                    throw new Exception("This stream has no defined size.");
            }

            /// <summary>
            ///* Finaliza a escrita do buffer.
            /// </summary>
            public void EndWrite()
            {
                SetPosition(0);
            }

            /// <summary>
            ///* Finaliza a escrita do buffer.
            /// </summary>
            public void Write()
            {
                if (!IsFixedSize())
                    EndWrite();
                else
                    EndWriteWithFixedCapacity();
            }

            /// <summary>
            ///* Obtém a cópia do buffer.
            /// </summary>
            /// <returns></returns>
            public byte[] ToArray()
            {
                if (!IsFixedSize())
                    return _stream.ToArray();
                else
                    return GetBuffer();
            }

            /// <summary>
            ///* Obtém o buffer interno do stream.
            /// </summary>
            /// <returns></returns>
            public byte[] GetBuffer()
            {
                return _stream.GetBuffer();
            }

            /// <summary>
            ///* Define a posição do fluxo.
            /// </summary>
            /// <param name="position"></param>
            public void SetPosition(int position)
            {
                _stream.Position = position;
            }

            /// <summary>
            ///* Obtém a posição do fluxo.
            /// </summary>
            /// <returns></returns>
            public long GetPosition()
            {
                return _stream.Position;
            }

            /// <summary>
            ///* Obtém a capacidade do fluxo.
            /// </summary>
            /// <returns></returns>
            public int GetCapacity()
            {
                return _stream.Capacity;
            }

            /// <summary>
            ///* Retorna se o tamanho do buffer é fixo.
            /// </summary>
            /// <returns></returns>
            public bool IsFixedSize()
            {
                return _isFixedSize;
            }

            /// <summary>
            ///* Define a capacidade do fluxo.
            /// </summary>
            /// <param name="size"></param>
            public void SetCapacity(int size)
            {
                _stream.Capacity = size;
            }

            /// <summary>
            ///* Limpa o stream para reutilização.
            /// </summary>
            public void Clear()
            {
                _stream.SetLength(0);
            }

            /// <summary>
            ///* Libera os recursos não gerenciados.
            /// </summary>
            public void Close()
            {
                _buffer = null;
                _stream.Dispose();
            }
        }
        public class IReader : INeutronReader
        {
            private byte[] _buffer = new byte[8]; //* Suporta até double(8 bytes).
            private readonly MemoryStream _stream;
            private readonly bool _isFixedSize;
            private int _bufferLength;
            public byte[] _internalBuffer;

            public IReader()
            {
                _stream = new MemoryStream();
            }

            public IReader(int capacity)
            {
                _stream = new MemoryStream(capacity);
                _isFixedSize = capacity > 0;
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

            public Quaternion ReadCompressedQuaternion(float FLOAT_PRECISION_MULT) // Copyright (c) 2016 StagPoint Software
            {
                var maxIndex = ReadByte();

                if (maxIndex >= 4 && maxIndex <= 7)
                {
                    var x = (maxIndex == 4) ? 1f : 0f;
                    var y = (maxIndex == 5) ? 1f : 0f;
                    var z = (maxIndex == 6) ? 1f : 0f;
                    var w = (maxIndex == 7) ? 1f : 0f;
                    return new Quaternion(x, y, z, w);
                }

                var a = ReadShort() / FLOAT_PRECISION_MULT;
                var b = ReadShort() / FLOAT_PRECISION_MULT;
                var c = ReadShort() / FLOAT_PRECISION_MULT;
                var d = Mathf.Sqrt(1f - (a * a + b * b + c * c));

                if (maxIndex == 0)
                    return new Quaternion(d, a, b, c);
                else if (maxIndex == 1)
                    return new Quaternion(a, d, b, c);
                else if (maxIndex == 2)
                    return new Quaternion(a, b, d, c);
                return new Quaternion(a, b, c, d);
            }

            public T[] ReadArray<T>()
            {
                byte[] buffer = ReadWithShort();
                T[] array = new T[buffer.Length / Marshal.SizeOf(typeof(T))];
                Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
                return array;
            }

            public T[] ReadArrayNext<T>()
            {
                byte[] buffer = ReadNext();
                T[] array = new T[buffer.Length / Marshal.SizeOf(typeof(T))];
                Buffer.BlockCopy(buffer, 0, array, 0, buffer.Length);
                return array;
            }

            public byte ReadPacket()
            {
                return ReadByte();
            }

            /// Ler o tamanho do pacote do cabeçalho do protocolo, com base no HEADER_TYPE.
            public byte[] ReadByteArrayWithAutoSize(out int size)
            {
                size = 0;
                switch (Helper.GetConstants().HeaderSize)
                {
                    case HeaderSizeType.Byte:
                        {
                            byte[] buffer = ReadWithByte(out byte sizeOf);
                            size = sizeOf;
                            return buffer;
                        }
                    case HeaderSizeType.Short:
                        {
                            byte[] buffer = ReadWithShort(out short sizeOf);
                            size = sizeOf;
                            return buffer;
                        }
                    case HeaderSizeType.Int:
                        {
                            byte[] buffer = ReadWithInteger(out int sizeOf);
                            size = sizeOf;
                            return buffer;
                        }
                    default:
                        return default;
                }
            }

            public byte[] ReadNext()
            {
                int bytesRemaining = (int)(_bufferLength - _stream.Position);
                return ReadBytes(bytesRemaining);
            }

            public T ReadWithInteger<T>()
            {
                return ReadWithInteger().Deserialize<T>();
            }

            public T ReadWithShort<T>()
            {
                return ReadWithShort().Deserialize<T>();
            }

            public T ReadWithByte<T>()
            {
                return ReadWithByte().Deserialize<T>();
            }

            public byte[] ReadWithInteger()
            {
                return ReadBytes(ReadInt());
            }

            public byte[] ReadWithShort()
            {
                return ReadBytes(ReadShort());
            }

            public byte[] ReadWithByte()
            {
                return ReadBytes(ReadByte());
            }

            public byte[] ReadWithInteger(out int size)
            {
                size = ReadInt();
                return ReadBytes(size);
            }

            public byte[] ReadWithShort(out short size)
            {
                size = ReadShort();
                return ReadBytes(size);
            }

            public byte[] ReadWithByte(out byte size)
            {
                size = ReadByte();
                return ReadBytes(size);
            }

            public byte ReadByte()
            {
                return (byte)_stream.ReadByte();
            }

            public bool ReadBool()
            {
                return ReadByte() != 0;
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

            public ushort ReadUShort()
            {
                Read(sizeof(ushort));
                return (ushort)(_buffer[0] | _buffer[1] << 8);
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

            public unsafe T Marshalling_ReadStructure<T>() where T : struct
            {
                byte[] data = ReadWithShort();
                fixed (byte* p = &data[0])
                    return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
            }

            public unsafe T Marshalling_ReadStructureNext<T>() where T : struct
            {
                byte[] data = ReadNext();
                fixed (byte* p = &data[0])
                    return (T)Marshal.PtrToStructure(new IntPtr(p), typeof(T));
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
                        throw new Exception("The stream is empty? or the write and read stream does not match? or the object has been disposed/recycled?");
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
                _internalBuffer = buffer;
                _bufferLength = _internalBuffer.Length;
                _stream.Write(_internalBuffer, 0, _bufferLength);
                SetPosition(0);
            }

            public void EndReadWithFixedCapacity()
            {
                if (_isFixedSize)
                    SetPosition(0);
                else
                    throw new Exception("This stream has no defined size.");
            }

            public void EndRead()
            {
                SetPosition(0);
            }

            public byte[] ToArray()
            {
                if (!_isFixedSize)
                    return _stream.ToArray();
                else
                    return GetBuffer();
            }

            public byte[] GetBuffer()
            {
                return _stream.GetBuffer();
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

            public bool IsFixedSize()
            {
                return _isFixedSize;
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
}