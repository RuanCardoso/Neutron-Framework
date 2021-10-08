using NeutronNetwork.Internal.Interfaces;
using UnityEngine;

namespace NeutronNetwork.Internal.Components
{
    public struct NetworkTransformSnapshot : ISnapshot
    {
        public double RemoteTimestamp {
            get;
            set;
        }
        public double LocalTimestamp {
            get;
            set;
        }

        public Vector3 position;
        public Quaternion rotation;
        public Vector3 scale;

        public NetworkTransformSnapshot(double remoteTimestamp, double localTimestamp, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            this.RemoteTimestamp = remoteTimestamp;
            this.LocalTimestamp = localTimestamp;
            this.position = position;
            this.rotation = rotation;
            this.scale = scale;
        }

        public static NetworkTransformSnapshot Interpolate(NetworkTransformSnapshot from, NetworkTransformSnapshot to, double t)
        {
            return new NetworkTransformSnapshot(0, 0, Vector3.LerpUnclamped(from.position, to.position, (float)t), Quaternion.SlerpUnclamped(from.rotation, to.rotation, (float)t), Vector3.LerpUnclamped(from.scale, to.scale, (float)t));
        }
    }
}