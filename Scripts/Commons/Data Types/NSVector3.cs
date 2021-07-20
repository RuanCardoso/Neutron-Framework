using System;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public struct NSVector3
    {
        public float x;
        public float y;
        public float z;

        public NSVector3(float x, float y, float z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public static implicit operator Vector3(NSVector3 serializableVector3)
        {
            return new Vector3(serializableVector3.x, serializableVector3.y, serializableVector3.z);
        }

        public static implicit operator NSVector3(Vector3 Vector3)
        {
            return new NSVector3(Vector3.x, Vector3.y, Vector3.z);
        }
    }
}