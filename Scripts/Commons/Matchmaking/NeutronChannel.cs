using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Json;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronChannel : MatchmakingBehaviour, INeutronSerializable, INeutron, IEquatable<NeutronChannel>, IEqualityComparer<NeutronChannel>
    {
        #region Fields
        [SerializeField] [ReadOnly] [HorizontalLine] [AllowNesting] private int roomCount;
        [SerializeField] private int _maxRooms;
        [SerializeField] [HorizontalLine] private RoomDictionary _rooms;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador do canal.
        /// </summary>
        public int ID { get => _id; set => _id = value; }
        /// <summary>
        ///* Retorna a quantidade de salas neste canal.
        /// </summary>
        public int RoomCount { get => roomCount; set => roomCount = value; }
        /// <summary>
        ///* Quantidade máxima de salas permitida neste canal.
        /// </summary>
        public int MaxRooms { get => _maxRooms; private set => _maxRooms = value; }
        #endregion

        public NeutronChannel() { }

        public NeutronChannel(int id, string name, int maxPlayers, string properties)
        {
            ID = id;
            Name = name;
            MaxPlayers = maxPlayers;
            Properties = properties;
        }

        public NeutronChannel(SerializationInfo info, StreamingContext context) // deserialization
        {
            ID = info.GetInt32("ID");
            Name = info.GetString("NM");
            PlayerCount = info.GetInt32("CP");
            roomCount = info.GetInt32("CR");
            MaxPlayers = info.GetInt32("MP");
            MaxRooms = info.GetInt32("MR");
            Properties = info.GetString("_");
            //////////////////////////////////////// Instantiate ////////////////////////////////////
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties);
            SceneView = new SceneView();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context) // serialization
        {
            info.AddValue("ID", ID);
            info.AddValue("NM", Name);
            info.AddValue("CP", PlayerCount);
            info.AddValue("CR", RoomCount);
            info.AddValue("MP", MaxPlayers);
            info.AddValue("MR", MaxRooms);
            info.AddValue("_", Properties);
        }

        public bool AddRoom(NeutronRoom room)
        {
            if (roomCount >= MaxRooms)
                return LogHelper.Error("Matchmaking: failed to enter, exceeded the maximum rooms limit.");
            else
            {
                bool TryValue;
                if ((TryValue = _rooms.TryAdd(room.ID, room)))
                    roomCount++;
                return TryValue;
            }
        }

        public bool GetRoom(string name)
        {
            foreach (var room in _rooms.Values)
            {
                if (room.Name == name)
                    return true;
                else
                    continue;
            }
            return false;
        }

        public NeutronRoom GetRoom(int index)
        {
            if (_rooms.TryGetValue(index, out NeutronRoom room))
                return room;
            else
                return null;
        }

        public NeutronRoom[] GetRooms()
        {
            return _rooms.Values.ToArray();
        }

        public Boolean Equals(NeutronChannel channel)
        {
            return this.ID == channel.ID;
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