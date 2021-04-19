using System;
using NeutronNetwork;

[Serializable]
public class RoomDictionary : NeutronSafeSerializableDictionary<Room>
{ }

[Serializable]
public class ChannelDictionary : NeutronSafeSerializableDictionary<Channel>
{ }

[Serializable]
public class PlayerDictionary : NeutronSafeSerializableDictionary<Player>
{ }