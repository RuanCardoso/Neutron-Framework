using System;

namespace NeutronNetwork.Internal.Wrappers
{
    [Serializable]
    public class RoomDictionary : NeutronSafeSerializableDictionary<NeutronRoom>
    { }

    [Serializable]
    public class ChannelDictionary : NeutronSafeSerializableDictionary<NeutronChannel>
    { }

    [Serializable]
    public class PlayerDictionary : NeutronSafeSerializableDictionary<NeutronPlayer>
    { }
}