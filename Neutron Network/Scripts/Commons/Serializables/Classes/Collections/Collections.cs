using System;
using NeutronNetwork;

[Serializable]
public class RoomDictionary : NeutronSafeSerializableDictionary<int, Room> { }

[Serializable]
public class ChannelDictionary : NeutronSafeSerializableDictionary<int, Channel> { }

[Serializable]
public class PlayerDictionary : NeutronSafeSerializableDictionary<int, Player> { }