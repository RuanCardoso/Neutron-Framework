using NeutronNetwork.Packets;
using System;

namespace NeutronNetwork.Server.Internal
{
    [Serializable]
    public class NeutronCache
    {
        public int Id {
            get;
            set;
        }
        public byte[] Buffer {
            get;
            set;
        }
        public NeutronPlayer Owner {
            get;
            set;
        }
        public CachedPacket Packet {
            get;
            set;
        }
        public CacheMode CacheMode {
            get;
            set;
        }

        public NeutronCache(int id, byte[] buffer, NeutronPlayer owner, CachedPacket packet, CacheMode cacheMode)
        {
            Id = id;
            Buffer = buffer;
            Owner = owner;
            Packet = packet;
            CacheMode = cacheMode;
        }
    }
}