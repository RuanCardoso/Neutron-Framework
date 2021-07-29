using NeutronNetwork.Helpers;
using System;
using System.IO;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronWriter : BinaryWriter, IDisposable
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
        ///* Inicializa uma nova instância, componente necessário para escrever bytes.
        /// </summary>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronWriter(bool recycle = true) : base(new MemoryStream())
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para escrever bytes.
        /// </summary>
        /// <param name="stream">* Define um stream personalizado.</param>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronWriter(MemoryStream stream, bool recycle = true) : base(stream)
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
            WriteExactly(buffer);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes outro fluxo de bytes <see cref="NeutronWriter"></see>.
        /// </summary>
        public void Write(NeutronWriter writer)
        {
            WriteExactly(writer.ToArray());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="Enum"></see>.
        /// </summary>
        public void WritePacket<T>(T packet) where T : Enum
        {
            Write((byte)(object)packet);
        }

        public void WriteFixedLength(int length)
        {
            Write(length);
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="T"></see>.<br/>
        ///* A instância <see cref="T"/> é serializada utilizando a serialização definida nas configurações.
        /// </summary>
        public void WriteExactly<T>(T obj)
        {
            WriteExactly(obj.Serialize());
        }

        /// <summary>
        ///* Escreve no fluxo de bytes uma instância do tipo <see cref="byte[]"></see>.<br/>
        /// </summary>
        public void WriteExactly(byte[] buffer)
        {
            WriteFixedLength(buffer.Length);
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
    }

    public class NeutronReader : BinaryReader, IDisposable
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
        public NeutronReader(bool recycle = true) : base(new MemoryStream())
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        /// <param name="recycle">* Define se o objeto deve ser reciclado ou descartado.</param>
        public NeutronReader(MemoryStream stream, bool recycle = true) : base(stream)
        {
            _memoryStream = stream;
            _recycle = recycle;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        public NeutronReader(byte[] buffer) : base(new MemoryStream(buffer))
        {
            _memoryStream = (MemoryStream)base.BaseStream;
            _recycle = false;
        }

        /// <summary>
        ///* Inicializa uma nova instância, componente necessário para ler o fluxo de bytes.
        /// </summary>
        public NeutronReader(byte[] buffer, int index, int count) : base(new MemoryStream(buffer, index, count))
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
            byte[] buffer = ReadExactly();
            float[] data = new float[buffer.Length / sizeof(float)];
            Buffer.BlockCopy(buffer, 0, data, 0, buffer.Length);
            return data;
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="Enum"></see> do fluxo de bytes.
        /// </summary>
        public T ReadPacket<T>() where T : Enum
        {
            return (T)(object)ReadByte();
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="T"></see> do fluxo de bytes e deserializa.
        /// </summary>
        public T ReadExactly<T>()
        {
            return ReadExactly().Deserialize<T>();
        }

        /// <summary>
        ///* Ler a instância do tipo <see cref="byte[]"></see> do fluxo de bytes.
        /// </summary>
        public byte[] ReadExactly()
        {
            return ReadBytes(ReadInt32());
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
            if (_recycle)
                SetPosition(0);
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
                Neutron.PooledNetworkReaders.Push(this);
            }
        }
    }
}