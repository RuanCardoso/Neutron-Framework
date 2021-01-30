using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public struct SerializableQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public SerializableQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Quaternion(SerializableQuaternion serializableQuaternion)
        {
            return new Quaternion(serializableQuaternion.x, serializableQuaternion.y, serializableQuaternion.z, serializableQuaternion.w);
        }

        public static implicit operator SerializableQuaternion(Quaternion quaternion)
        {
            return new SerializableQuaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }
    }
}