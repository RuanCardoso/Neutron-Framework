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

        public NeutronChannel(int id, string name, int maxPlayers, string properties) : base(name, maxPlayers, properties)
        {
            ID = id;
        }

        public NeutronChannel(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            ID = info.GetInt32("id");
            RoomCount = info.GetInt32("roomCount");
            MaxRooms = info.GetInt32("maxRooms");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            {
                info.AddValue("id", ID);
                info.AddValue("roomCount", RoomCount);
                info.AddValue("maxRooms", MaxRooms);
            }
        }

        public bool AddRoom(NeutronRoom room)
        {
            if (RoomCount >= MaxRooms)
                return LogHelper.Error("Failed to enter, exceeded the maximum rooms limit.");
            else
            {
                bool TryValue;
                if ((TryValue = _rooms.TryAdd(room.ID, room)))
                    RoomCount++;
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

        public NeutronRoom[] GetRooms(Func<NeutronRoom, bool> predicate)
        {
            return _rooms.Values.Where(predicate).ToArray();
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