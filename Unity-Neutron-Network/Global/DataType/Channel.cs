using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Collections.Concurrent;
using System.Runtime.Serialization;
using System.Threading;
using UnityEngine;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Client;
using NeutronNetwork.Internal;

namespace NeutronNetwork
{
    [Serializable]
    public class Channel : IEquatable<Channel>, INotify, IEqualityComparer<Channel>
    {
        [NonSerialized] private readonly object SyncBuffer = new object();
        [NonSerialized] private readonly object SyncRooms = new object();
        [NonSerialized] private readonly object SyncPlayers = new object();
        /// <summary>
        /// ID of channel.
        /// </summary>
        public int ID { get => iD; set => iD = value; }
        [SerializeField] private int iD;
        /// <summary>
        /// Name of channel.
        /// </summary>
        public string Name { get => name; set => name = value; }
        [SerializeField] private string name;
        /// <summary>
        /// Current amount of players serialized in inspector.
        /// </summary>
        public int CountOfPlayers
        {
            get
            {
                lock (SyncPlayers)
                {
                    return countOfPlayers;
                }
            }
        }
        [SerializeField, ReadOnly] private int countOfPlayers; // Only show in inspector.
        /// <summary>
        /// Current amount of rooms serialized in inspector.
        /// </summary>
        public int CountOfRooms
        {
            get
            {
                lock (SyncRooms)
                {
                    return countOfRooms;
                }
            }
        }
        [SerializeField, ReadOnly] private int countOfRooms; // Only show in inspector.
        /// <summary>
        /// Max Players of channel.
        /// </summary>
        public int MaxPlayers { get => maxPlayers; set => maxPlayers = value; }
        [SerializeField] private int maxPlayers; // Thread safe. Immutable
        /// <summary>
        /// Max rooms of channel.
        /// </summary>
        public int MaxRooms { get => maxRooms; set => maxRooms = value; }
        [SerializeField] private int maxRooms; // Thread safe. Immutable
        /// <summary>
        /// Properties of channel(JSON).
        /// </summary>
        public string ___props { get => properties; set => properties = value; }
        [SerializeField, TextArea] private string properties = "{\"\":\"\"}";
        /// <summary>
        /// Properties of channel.
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public Dictionary<string, object> GetProps { get; set; }
        /// <summary>
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
        [NonSerialized] private List<CachedBuffer> CachedPackets = new List<CachedBuffer>(); // not thread safe, requires locking.
        /// <summary>
        /// list of players.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
#if !UNITY_EDITOR
        [NonSerialized]
#else
        [SerializeField]
#endif
        private List<Player> Players = new List<Player>(); // not thread safe, requires locking.
        /// <summary>
        /// list of rooms.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
#if !UNITY_EDITOR
        [NonSerialized]
#else
        [SerializeField]
#endif
        private List<Room> Rooms = new List<Room>(); // not thread safe, requires locking.

        public Channel() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Channel(int ID, string Name, int maxPlayers, string properties)
        {
            this.ID = ID;
            this.Name = Name;
            this.MaxPlayers = maxPlayers;
            this.___props = properties;
        }

        public bool AddPlayer(Player player, out string errorMessage) // [Thread-Safe]
        {
            lock (SyncPlayers) // Thread safe.
            {
                if (countOfPlayers >= MaxPlayers) // Thread-Safe - check if CountOfPlayers(Interlocked) > maxplayers(Immutable)
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

        public bool RemovePlayer(Player player)
        {
            lock (SyncPlayers) // Thread safe.
            {
                countOfPlayers--;
                return Players.Remove(player);
            }
        }

        public bool AddRoom(Room room, out string errorMessage) // [Thread-Safe]
        {
            lock (SyncRooms) // Thread safe.
            {
                if (countOfRooms >= MaxRooms) // Thread-Safe - check if CountOfPlayers(Interlocked) > maxplayers(Immutable)
                { errorMessage = "The Channel is full."; return false; }
                else
                {
                    Rooms.Add(room); // add player in channel;
                    bool added = Rooms.Contains(room);
                    if (added)
                    {
                        countOfRooms++;
                        errorMessage = "OK";
                        return true;
                    }
                }
                errorMessage = "It was not possible to create this room.";
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

        public bool RoomExists(string name)
        {
            lock (SyncRooms)
            {
                foreach (var room in Rooms.ToArray())
                {
                    if (room.Name == name) return true;
                    else continue;
                }
                return false;
            }
        }

        public void AddCache(CachedBuffer buffer)
        {
            lock (SyncBuffer)
            {
                CachedPackets.Add(buffer);
            }
        }

        public CachedBuffer[] GetCaches()
        {
            lock (SyncBuffer)
            {
                return CachedPackets.ToArray();
            }
        }

        public Room GetRoom(int index)
        {
            lock (SyncRooms)
            {
                foreach (var room in Rooms.ToArray())
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
    }
}