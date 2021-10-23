using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class NeutronChannel : MatchmakingBehaviour, INeutronSerializable, INeutronIdentify, IEquatable<NeutronChannel>, IEqualityComparer<NeutronChannel>
    {
        #region Fields
        [SerializeField] [ReadOnly] [HorizontalLine] [AllowNesting] private int _roomCount;
        [SerializeField] private int _maxRooms;
        [SerializeField] [HorizontalLine] private RoomDictionary _rooms;
        #endregion

        #region Properties
        /// <summary>
        ///* Retorna o identificador do canal.
        /// </summary>
        public int Id {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        ///* Retorna a quantidade de salas neste canal.
        /// </summary>
        public int RoomCount {
            get => _roomCount;
        }

        /// <summary>
        ///* Quantidade máxima de salas permitida neste canal.
        /// </summary>
        public int MaxRooms {
            get => _maxRooms;
        }
        #endregion

        public NeutronChannel() { }

        public NeutronChannel(int id, string name, int maxPlayers, string properties) : base(name, maxPlayers, properties)
        {
            Id = id;
        }

        public NeutronChannel(SerializationInfo info, StreamingContext context) : base(info, context)
        {
            Id = info.GetInt32("id");
            _roomCount = info.GetInt32("roomCount");
            _maxRooms = info.GetInt32("maxRooms");
        }

        public override void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            base.GetObjectData(info, context);
            {
                info.AddValue("id", Id);
                info.AddValue("roomCount", RoomCount);
                info.AddValue("maxRooms", MaxRooms);
            }
        }

        public override void Apply(NeutronChannel channel)
        {
            base.Apply(channel);
            _id = channel.Id;
            _roomCount = channel.RoomCount;
        }

        public bool Add(NeutronRoom room)
        {
            if (RoomCount >= MaxRooms)
                return LogHelper.Error("It was not possible to create the room because the maximum limit of rooms was exceeded.");
            else
            {
                bool TryValue;
                if ((TryValue = _rooms.TryAdd(room.Id, room)))
                    _roomCount++;
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

        public bool Equals(NeutronChannel channel)
        {
            return this.Id == channel.Id;
        }

        public bool Equals(NeutronChannel x, NeutronChannel y)
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
            return x.Id == y.Id;
        }

        public int GetHashCode(NeutronChannel obj)
        {
            return obj.Id.GetHashCode();
        }

        public override void OnAfterDeserialize()
        {
            base.OnAfterDeserialize();
            {
#if UNITY_EDITOR
                if (_roomCount != _rooms.Count)
                    _roomCount = _rooms.Count;
                if (_maxRooms < _roomCount)
                    _maxRooms = _roomCount;
#endif
            }
        }
    }
}