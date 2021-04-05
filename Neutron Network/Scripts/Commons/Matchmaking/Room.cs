using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class Room : ANeutronMatchmaking, INeutronSerializable, INeutronNotify, IEquatable<Room>, IEqualityComparer<Room>
    {
        /// <summary>
        ///* Unique room ID.
        /// </summary>
        public int ID { get => m_ID; set => m_ID = value; }
        /// <summary>
        ///* Check if room has password.
        /// </summary>
        public bool HasPassword { get => m_HasPassword; set => m_HasPassword = value; }
        [SerializeField, ReadOnly] private bool m_HasPassword;
        /// <summary>
        ///* Check if room is visible.
        /// </summary>
        public bool IsVisible { get => m_IsVisible; set => m_IsVisible = value; }
        [SerializeField] private bool m_IsVisible;

        public Room() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Room(int ID, string roomName, int maxPlayers, bool hasPassword, bool isVisible, string options)
        {
            this.ID = ID;
            this.Name = roomName;
            this.MaxPlayers = maxPlayers;
            this.HasPassword = hasPassword;
            this.IsVisible = isVisible;
            this._ = options;
        }

        public Room(SerializationInfo info, StreamingContext context)
        {
            ID = info.GetInt32("ID");
            Name = info.GetString("NM");
            CountOfPlayers = info.GetInt32("CP");
            MaxPlayers = info.GetInt32("MP");
            HasPassword = info.GetBoolean("HP");
            IsVisible = info.GetBoolean("IV");
            _ = info.GetString("_");
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(_);
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID);
            info.AddValue("NM", Name);
            info.AddValue("CP", CountOfPlayers);
            info.AddValue("MP", MaxPlayers);
            info.AddValue("HP", HasPassword);
            info.AddValue("IV", IsVisible);
            info.AddValue("_", _);
        }

        public Boolean Equals(Room other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(Room x, Room y)
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

        public Int32 GetHashCode(Room obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}