using System;
using System.Collections.Generic;
using UnityEngine;
using NeutronNetwork.Internal.Attributes;
using System.Runtime.Serialization;
using NeutronNetwork.Json;
using System.Linq;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Interfaces;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronChannel : MatchmakingBehaviour, INeutronSerializable, INeutron, IEquatable<NeutronChannel>, IEqualityComparer<NeutronChannel>
    {
        /// <summary>
        ///* ID of channel.
        /// </summary>
        public int ID { get => m_ID; set => m_ID = value; }
        /// <summary>
        ///* Current amount of rooms.
        /// </summary>
        public int CountOfRooms { get => m_CountOfRooms; set => m_CountOfRooms = value; }
        [SerializeField] [ReadOnly] private int m_CountOfRooms;
        /// <summary>
        ///* Max rooms of channel.
        /// </summary>
        public int MaxRooms { get => m_MaxRooms; private set => m_MaxRooms = value; }
        [SerializeField] [HorizontalLineDown] private int m_MaxRooms;
        /// <summary>
        ///* list of rooms.
        /// </summary>
        [SerializeField] private RoomDictionary Rooms;

        public NeutronChannel() { }

        public NeutronChannel(int ID, string Name, int maxPlayers, string properties)
        {
            this.ID = ID;
            this.Name = Name;
            this.MaxPlayers = maxPlayers;
            this._ = properties;
        }

        public NeutronChannel(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Name = info.GetString("NM");
            CountOfPlayers = info.GetInt32("CP");
            m_CountOfRooms = info.GetInt32("CR");
            MaxPlayers = info.GetInt32("MP");
            MaxRooms = info.GetInt32("MR");
            _ = info.GetString("_");
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(_);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NM", Name);
            info.AddValue("CP", CountOfPlayers);
            info.AddValue("CR", CountOfRooms);
            info.AddValue("MP", MaxPlayers);
            info.AddValue("MR", MaxRooms);
            info.AddValue("_", _);
        }

        public bool AddRoom(NeutronRoom room)
        {
            if (m_CountOfRooms >= MaxRooms)
                return LogHelper.Error("Matchmaking: failed to enter, exceeded the maximum rooms limit.");
            else
            {
                bool TryValue = false;
                if ((TryValue = Rooms.TryAdd(room.ID, room)))
                    m_CountOfRooms++;
                return TryValue;
            }
        }

        public bool RoomExists(string name)
        {
            //lock (SyncRooms)
            {
                foreach (var room in Rooms.Values)
                {
                    if (room.Name == name) return true;
                    else continue;
                }
                return false;
            }
        }

        public NeutronRoom GetRoom(int index)
        {
            if (Rooms.TryGetValue(index, out NeutronRoom l_Room))
                return l_Room;
            else return null;
        }

        public NeutronRoom[] GetRooms()
        {
            return Rooms.Values.ToArray();
        }

        public Boolean Equals(NeutronChannel other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(NeutronChannel x, NeutronChannel y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return true;
            }
            if (object.ReferenceEquals(x, null) ||
                object.ReferenceEquals(y, null))
            {
                return false;
            }
            return x.ID == y.ID;
        }

        public Int32 GetHashCode(NeutronChannel obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}