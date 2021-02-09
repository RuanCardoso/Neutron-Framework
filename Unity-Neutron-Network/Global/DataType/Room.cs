using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class Room : IEquatable<Room>, INotify, ISerializable, IEqualityComparer<Room>
    {
        private readonly object SyncPlayers = new object(); // sync
        /// <summary>
        /// ID of channel.
        /// </summary>
        [SerializeField] private int iD;
        /// <summary>
        /// ID of channel.
        /// </summary>
        public int ID { get => iD; }
        /// <summary>
        /// Name of room.
        /// </summary>
        public string Name;
        /// <summary>
        /// Current amount of players
        /// </summary>
        [SerializeField, ReadOnly] private int countOfPlayers;
        public int CountOfPlayers {
            get {
                return countOfPlayers;
            }
        }
        // <summary>
        /// Max Players of room.
        /// </summary>
        public int maxPlayers;
        /// <summary>
        /// Check if room has password.
        /// </summary>
        [ReadOnly] public bool hasPassword;
        /// <summary>
        /// Check if room is visible.
        /// </summary>
        public bool isVisible;
        /// <summary>
        /// owner of room.
        /// </summary>
        [NonSerialized] public Player Owner;
        /// <summary>
        /// Properties of channel.
        /// </summary>
        [SerializeField, TextArea] private string properties = "{\"\":\"\"}";
        /// <summary>
        /// Properties of channel.
        /// </summary>
        [NonSerialized] public Dictionary<string, object> Properties;
        /// <summary>
        /// list of players.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
        [SerializeField] private List<Player> Players = new List<Player>(); // not thread safe, requires locking.

        public Room() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Room(int ID, string roomName, int maxPlayers, bool hasPassword, bool isVisible, string options)
        {
            this.iD = ID;
            this.Name = roomName;
            this.maxPlayers = maxPlayers;
            this.hasPassword = hasPassword;
            this.isVisible = isVisible;
            this.properties = options;
        }

        public Room(SerializationInfo info, StreamingContext context)
        {
            iD = (int)info.GetValue("ID", typeof(int));
            Name = (string)info.GetValue("Name", typeof(string));
            countOfPlayers = (int)info.GetValue("countPlayers", typeof(int));
            maxPlayers = (int)info.GetValue("maxPlayers", typeof(int));
            hasPassword = (bool)info.GetValue("hasPassword", typeof(bool));
            isVisible = (bool)info.GetValue("isVisible", typeof(bool));
            properties = (string)info.GetValue("properties", typeof(string));
            int code = Utils.ValidateAndDeserializeJson(properties, out Dictionary<string, object> _properties);
            switch (code)
            {
                case 1:
                    Properties = _properties;
                    break;
                case 2:
                    Utils.LoggerError($"Properties is empty -> Room: [{ID}]");
                    break;
                case 0:
                    Utils.LoggerError($"Invalid JSON error -> Room: [{ID}]");
                    break;
            }
        }

        public bool AddPlayer(Player player, out string errorMessage)
        {
            lock (SyncPlayers) // Thread safe.
            {
                if (countOfPlayers >= maxPlayers) // Thread-Safe - check if CountOfPlayers(Interlocked) > maxplayers(Immutable)
                { errorMessage = "The Room is full."; return false; }
                else
                {
                    Players.Add(player); // add player in channel;
                    bool added = Players.Contains(player);
                    if (added)
                    {
                        countOfPlayers++;
                        errorMessage = "OK";
                        return true;
                    }
                }
                errorMessage = "It was not possible to enter this room.";
                return false;
            }
        }

        public Player[] GetPlayers()
        {
            lock (SyncPlayers)
            {
                return Players.ToArray();
            }
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

        public void SetProperties(string props) // [THREAD-SAFE - player is individual]
        {
            properties = props;
        }

        public Int32 GetHashCode(Room obj)
        {
            return obj.ID.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID, typeof(int));
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("countPlayers", countOfPlayers, typeof(int));
            info.AddValue("maxPlayers", maxPlayers, typeof(int));
            info.AddValue("hasPassword", hasPassword, typeof(bool));
            info.AddValue("isVisible", isVisible, typeof(bool));
            info.AddValue("properties", properties, typeof(string));
        }
    }
}