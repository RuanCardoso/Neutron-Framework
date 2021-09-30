using NeutronNetwork.Internal.Interfaces;
using System;
using UnityEngine;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronWriter : INeutronStream
    {
        void EndWriteWithFixedCapacity();
        void EndWrite();
        void Finish();
        void Write(Byte value);
        void Write(bool value);
        void Write(Byte[] buffer);
        void Write(Byte[] buffer, Int32 offset, Int32 size);
        void Write(Color color);
        void Write(Double value);
        void Write(Single value);
        void Write(Int32 value);
        void Write(Quaternion quaternion);
        void Write(Int16 value);
        void Write(String text);
        void Write(Vector2 vector);
        void Write(Vector3 vector);
        void Write<T>(T[] array, Int32 sizeOf);
        void WriteSize(byte[] buffer);
        void Write7BitEncodedInt(Int32 value);
        void WriteByteExactly(Byte[] buffer);
        void WriteByteExactly<T>(T obj);
        void WriteByteWriter(NeutronStream.IWriter writer);
        void WriteIntExactly(Byte[] buffer);
        void WriteIntExactly<T>(T obj);
        void WriteIntWriter(NeutronStream.IWriter writer);
        void WriteNextBytes(Byte[] buffer);
        void WriteNextBytes(NeutronStream.IWriter writer);
        void WritePacket(byte packet);
        void WriteShortExactly(Byte[] buffer);
        void WriteShortExactly<T>(T obj);
        void WriteShortWriter(NeutronStream.IWriter writer);
    }
}