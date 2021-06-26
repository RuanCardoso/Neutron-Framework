using System;
using NeutronNetwork;

namespace NeutronNetwork.Internal.Wrappers
{
    [Serializable]
    public class RoomDictionary : NeutronSafeSerializableDictionary<Room>
    { }

    [Serializable]
    public class ChannelDictionary : NeutronSafeSerializableDictionary<Channel>
    { }

    [Serializable]
    public class PlayerDictionary : NeutronSafeSerializableDictionary<Player>
    { }
}