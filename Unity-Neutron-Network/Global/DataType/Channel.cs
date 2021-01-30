using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;

namespace NeutronNetwork
{
    [Serializable]
    public class Channel : IEquatable<Channel>, INotify, ISerializable, IEqualityComparer<Channel>
    {
        private readonly object SyncPackets = new object(); // sync
        private readonly object SyncRooms = new object(); // sync
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
        /// Name of channel.
        /// </summary>
        public string Name;
        /// <summary>
        /// Current amount of players serialized in inspector.
        /// </summary>
        [SerializeField, ReadOnly] private int countOfPlayers; // Only show in inspector.
        public int CountOfPlayers {
            get {
                return countOfPlayers;
            }
        }
        /// <summary>
        /// Max Players of channel.
        /// </summary>
        public int maxPlayers; // Thread safe. Immutable
        /// <summary>
        /// Properties of channel(JSON).
        /// </summary>
        [SerializeField, TextArea] private string properties = "{\"\":\"\"}";
        /// <summary>
        /// Properties of channel.
        /// </summary>
        public Dictionary<string, object> Properties; // not thread safe, requires locking? no....
        /// <summary>
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
        public List<CachedBuffer> _cachedPackets = new List<CachedBuffer>(); // not thread safe, requires locking.
        /// <summary>
        /// list of players.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
        [SerializeField] private List<Player> Players = new List<Player>(); // not thread safe, requires locking.
        /// <summary>
        /// list of rooms.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
        [SerializeField] private List<Room> Rooms = new List<Room>(); // not thread safe, requires locking.

        public Channel() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Channel(int ID, string Name, int maxPlayers, string properties)
        {
            this.iD = ID;
            this.Name = Name;
            this.maxPlayers = maxPlayers;
            this.properties = properties;
        }

        public Channel(SerializationInfo info, StreamingContext context) // deserialization constructor.
        {
            iD = (int)info.GetValue("ID", typeof(int));
            Name = (string)info.GetValue("Name", typeof(string));
            countOfPlayers = (int)info.GetValue("countPlayers", typeof(int));
            maxPlayers = (int)info.GetValue("maxPlayers", typeof(int));
            properties = (string)info.GetValue("properties", typeof(string));
            int code = Utils.ValidateAndDeserializeJson(properties, out Dictionary<string, object> _properties);
            switch (code)
            {
                case 1:
                    Properties = _properties;
                    break;
                case 2:
                    Utils.LoggerError($"Properties is empty -> Channel: [{ID}]");
                    break;
                case 0:
                    Utils.LoggerError($"Invalid JSON error -> Channel: [{ID}]");
                    break;
            }
        }

        public bool AddPlayer(Player player, out string errorMessage)
        {
            lock (SyncPlayers) // Thread safe.
            {
                if (countOfPlayers >= maxPlayers) // Thread-Safe - check if CountOfPlayers(Interlocked) > maxplayers(Immutable)
                { errorMessage = "The Channel is full."; return false; }
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
                errorMessage = "It was not possible to enter this channel.";
                return false;
            }
        }

        public Room GetRoom(int index)
        {
            lock (SyncRooms)
            {
                foreach (var room in Rooms)
                {
                    if (room.ID == index) return room;
                    else continue;
                }
                return null;
            }
        }

        public Room[] GetRooms()
        {
            lock (SyncRooms)
            {
                return Rooms.ToArray();
            }
        }

        public Boolean Equals(Channel other)
        {
            return this.ID == other.ID;
        }

        public Boolean Equals(Channel x, Channel y)
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

        public Int32 GetHashCode(Channel obj)
        {
            return obj.ID.GetHashCode();
        }

        public void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("ID", ID, typeof(int));
            info.AddValue("Name", Name, typeof(string));
            info.AddValue("countPlayers", countOfPlayers, typeof(int));
            info.AddValue("maxPlayers", maxPlayers, typeof(int));
            info.AddValue("properties", properties, typeof(string));
        }
    }
}