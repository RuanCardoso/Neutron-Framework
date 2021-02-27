using NeutronNetwork.Internal;
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
    public class Room : IEquatable<Room>, INotify, IEqualityComparer<Room>
    {
        [NonSerialized] private readonly object SyncPlayers = new object();
        /// <summary>
        /// ID of channel.
        /// </summary>
        public int ID { get => iD; set => iD = value; }
        [SerializeField] private int iD;
        /// Name of room.
        /// </summary>
        public string Name { get => name; set => name = value; }
        [SerializeField] private string name;
        /// <summary>
        /// Current amount of players
        /// </summary>  
        public int CountOfPlayers { get => countOfPlayers; }
        [SerializeField, ReadOnly] private int countOfPlayers;
        /// <summary>
        /// Max Players of room.
        /// </summary>
        public int MaxPlayers { get => maxPlayers; set => maxPlayers = value; }
        [SerializeField] private int maxPlayers;
        /// <summary>
        /// Check if room has password.
        /// </summary>
        public bool HasPassword { get => hasPassword; set => hasPassword = value; }
        [SerializeField, ReadOnly] private bool hasPassword;
        /// <summary>
        /// Check if room is visible.
        /// </summary>
        public bool IsVisible { get => isVisible; set => isVisible = value; }
        [SerializeField] private bool isVisible;
        /// <summary>
        /// owner of room.
        /// </summary>
        [field: NonSerialized]
        [JsonIgnore]
        public Player Owner { get; set; }
        /// <summary>
        /// Properties of channel.
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
        /// list of players.
        /// returns null on the client.
        /// not serialized over the network
        /// </summary>
#if !UNITY_EDITOR
        [NonSerialized]
#else
        [SerializeField]
#endif
        private List<Player> Players = new List<Player>();

        public Room() { } // the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public Room(int ID, string roomName, int maxPlayers, bool hasPassword, bool isVisible, string options)
        {
            this.iD = ID;
            this.Name = roomName;
            this.MaxPlayers = maxPlayers;
            this.HasPassword = hasPassword;
            this.IsVisible = isVisible;
            this.___props = options;
        }

        public bool AddPlayer(Player player, out string errorMessage)
        {
            lock (SyncPlayers) // Thread safe.
            {
                if (countOfPlayers >= MaxPlayers) // Thread-Safe - check if CountOfPlayers(Interlocked) > maxplayers(Immutable)
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

        public bool RemovePlayer(Player player)
        {
            lock (SyncPlayers) // Thread safe.
            {
                countOfPlayers--;
                return Players.Remove(player);
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

        public Int32 GetHashCode(Room obj)
        {
            return obj.ID.GetHashCode();
        }
    }
}