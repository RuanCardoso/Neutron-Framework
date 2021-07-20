using NeutronNetwork.Attributes;
using NeutronNetwork.Interfaces;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
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
    public class NeutronRoom : MatchmakingBehaviour, INeutronSerializable, INeutron, IEquatable<NeutronRoom>, IEqualityComparer<NeutronRoom>
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

        public NeutronRoom() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public NeutronRoom(int ID, string roomName, int maxPlayers, bool hasPassword, bool isVisible, string options)
        {
            this.ID = ID;
            this.Name = roomName;
            this.MaxPlayers = maxPlayers;
            this.HasPassword = hasPassword;
            this.IsVisible = isVisible;
            this._ = options;
        }

        public NeutronRoom(SerializationInfo info, StreamingContext context)
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

        public Boolean Equals(NeutronRoom other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(NeutronRoom x, NeutronRoom y)
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

        public Int32 GetHashCode(NeutronRoom obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}