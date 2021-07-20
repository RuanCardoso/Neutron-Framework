using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public struct NSQuaternion
    {
        public float x;
        public float y;
        public float z;
        public float w;

        public NSQuaternion(float x, float y, float z, float w)
        {
            this.x = x;
            this.y = y;
            this.z = z;
            this.w = w;
        }

        public static implicit operator Quaternion(NSQuaternion serializableQuaternion)
        {
            return new Quaternion(serializableQuaternion.x, serializableQuaternion.y, serializableQuaternion.z, serializableQuaternion.w);
        }

        public static implicit operator NSQuaternion(Quaternion quaternion)
        {
            return new NSQuaternion(quaternion.x, quaternion.y, quaternion.z, quaternion.w);
        }
    }
}