using NeutronNetwork.Internal.Interfaces;
using System;
using UnityEngine;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronWriter : INeutronStream
    {
        void EndWriteWithFixedCapacity();
        void EndWrite();
        void Write();
        void Write(Byte value);
        void Write(bool value);
        void Write(Byte[] buffer);
        void Write(Byte[] buffer, Int32 offset, Int32 size);
        void Write(Color color);
        void Write(Double value);
        void Write(Single value);
        void Write(Int32 value);
        void Write(Quaternion quaternion);
        void WriteCompressed(Quaternion quaternion, float FLOAT_PRECISION_MULT);
        void WriteCompressed(Vector3 vector3);
        void Write(Int16 value);
        void Write(UInt16 value);
        void Write(String text);
        void Write(Vector2 vector);
        void Write(Vector3 vector);
        void Write<T>(T[] array);
        void WriteNext<T>(T[] array);
        void WriteByteArrayWithAutoSize(byte[] buffer);
        void Write7BitEncodedInt(Int32 value);
        void WriteWithByte(Byte[] buffer);
        void WriteWithByte<T>(T obj);
        void WriteWithByte(NeutronStream.IWriter writer);
        void WriteWithInteger(Byte[] buffer);
        void WriteWithInteger<T>(T obj);
        void WriteWithInteger(NeutronStream.IWriter writer);
        void WriteNext(Byte[] buffer);
        void WriteNext(NeutronStream.IWriter writer);
        void WritePacket(byte packet);
        void WriteWithShort(Byte[] buffer);
        void WriteWithShort<T>(T obj);
        void WriteWithShort(NeutronStream.IWriter writer);
        void Marshalling_Write<T>(T structure) where T : struct;
        void Marshalling_WriteNext<T>(T structure) where T : struct;
    }
}