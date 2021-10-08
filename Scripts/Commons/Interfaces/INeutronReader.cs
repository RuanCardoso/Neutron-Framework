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
        T[] ReadArray<T>();
        T[] ReadArrayNext<T>();
        Byte ReadByte();
        Boolean ReadBool();
        Byte[] ReadWithByte();
        Byte[] ReadWithByte(out Byte size);
        T ReadWithByte<T>();
        Byte[] ReadBytes(Int32 size);
        Color ReadColor();
        Double ReadDouble();
        Single ReadFloat();
        Int32 ReadInt();
        Byte[] ReadWithInteger();
        Byte[] ReadWithInteger(out Int32 size);
        T ReadWithInteger<T>();
        Byte[] ReadNext();
        Byte ReadPacket();
        Quaternion ReadQuaternion();
        Quaternion ReadCompressedQuaternion(float FLOAT_PRECISION_MULT);
        Int16 ReadShort();
        UInt16 ReadUShort();
        Byte[] ReadWithShort();
        Byte[] ReadWithShort(out Int16 size);
        T ReadWithShort<T>();
        Byte[] ReadByteArrayWithAutoSize(out Int32 size);
        String ReadString();
        Vector2 ReadVector2();
        Vector3 ReadVector3();
        void SetBuffer(Byte[] buffer);
        T Marshalling_ReadStructure<T>() where T : struct;
        T Marshalling_ReadStructureNext<T>() where T : struct;
    }
}