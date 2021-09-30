using System;
using UnityEngine;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronReader : INeutronStream
    {
        void EndReadWithFixedCapacity();
        void EndRead();
        void Read(Int32 size, Byte[] buffer = null);
        Int32 Read7BitEncodedInt();
        T[] ReadArray<T>(Int32 sizeOf, Int32 length);
        Byte ReadByte();
        Boolean ReadBool();
        Byte[] ReadByteExactly();
        Byte[] ReadByteExactly(out Byte size);
        T ReadByteExactly<T>();
        Byte[] ReadBytes(Int32 size);
        Color ReadColor();
        Double ReadDouble();
        Single ReadFloat();
        Int32 ReadInt();
        Byte[] ReadIntExactly();
        Byte[] ReadIntExactly(out Int32 size);
        T ReadIntExactly<T>();
        Byte[] ReadNextBytes(Int32 size);
        Byte ReadPacket();
        Quaternion ReadQuaternion();
        Int16 ReadShort();
        Byte[] ReadShortExactly();
        Byte[] ReadShortExactly(out Int16 size);
        T ReadShortExactly<T>();
        Byte[] ReadSize(out Int32 size);
        String ReadString();
        Vector2 ReadVector2();
        Vector3 ReadVector3();
        void SetBuffer(Byte[] buffer);
    }
}