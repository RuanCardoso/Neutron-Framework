using System.Collections.Generic;
using System.Linq;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Server.Internal;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public class MatchmakingBehaviour : INeutronMatchmaking
    {
        private int m_UniqueBufferID = 0;
        [SerializeField] [ReadOnly] protected int m_ID;
        /// <summary>
        ///* Name of channel.
        /// </summary>
        public string Name { get => m_Name; set => m_Name = value; }
        [SerializeField] private string m_Name;
        /// <summary>
        ///* Current amount of players serialized in inspector.
        /// </summary>
        public int CountOfPlayers { get => m_CountOfPlayers; set => m_CountOfPlayers = value; }
        [SerializeField, ReadOnly] private int m_CountOfPlayers; // Only show in inspector.
        /// <summary>
        ///* Max Players of channel.
        /// </summary>
        public int MaxPlayers { get => m_MaxPlayers; set => m_MaxPlayers = value; }
        [SerializeField] private int m_MaxPlayers; // Thread safe. Immutable
        /// <summary>
        ///* Properties of channel(JSON).
        /// </summary>
        public string _ { get => m_Properties; set => m_Properties = value; }
        [SerializeField] [HorizontalLineDown] private string m_Properties = "{\"Neutron\":\"Neutron\"}";
        /// <summary>
        ///* Owner of room.
        /// </summary>
        public NeutronPlayer Owner { get; set; }
        /// <summary>
        ///* Properties of channel.
        /// </summary>
        public Dictionary<string, object> Get { get; set; }
        /// <summary>
        ///* cache of players.
        /// </summary>
        private Dictionary<(int, int), NeutronCache> CachedPackets => m_CachedPackets;
        private Dictionary<(int, int), NeutronCache> m_CachedPackets = new Dictionary<(int, int), NeutronCache>();
        /// <summary>
        ///* list of players.
        /// </summary>
        private PlayerDictionary Players => m_Players;
        [SerializeField] [ReadOnly] private PlayerDictionary m_Players;
        /// <summary>
        ///* Scene settings.
        /// </summary>
        public SceneView SceneSettings { get => m_SceneSettings; set => m_SceneSettings = value; }
        [SerializeField] [HorizontalLineDown] private SceneView m_SceneSettings;

        public bool AddPlayer(NeutronPlayer player)
        {
            if (CountOfPlayers >= MaxPlayers)
                return LogHelper.Error("Matchmaking: failed to enter, exceeded the maximum players limit.");
            else
            {
                bool TryValue;
                if ((TryValue = Players.TryAdd(player.ID, player)))
                    CountOfPlayers++;
                return TryValue;
            }
        }

        public bool RemovePlayer(NeutronPlayer player)
        {
            bool TryValue;
            if ((TryValue = Players.TryRemove(player.ID, out NeutronPlayer _)))
                CountOfPlayers--;
            return TryValue;
        }

        public NeutronPlayer[] GetPlayers()
        {
            return Players.Values.ToArray();
        }

        public void AddCache(NeutronCache buffer)
        {
            if (buffer.Cache == Cache.Overwrite)
            {
                (int, int) generateUniqueKey = (buffer.Id, buffer.Owner.ID);
                if (CachedPackets.ContainsKey(generateUniqueKey))
                    CachedPackets[generateUniqueKey] = buffer;
                else CachedPackets.Add(generateUniqueKey, buffer);
            }
            else if (buffer.Cache == Cache.New)
            {
                (int, int) generateUniqueKey = (buffer.Owner.ID, ++m_UniqueBufferID);
                CachedPackets.Add(generateUniqueKey, buffer);
            }
        }

        public NeutronCache[] GetCaches()
        {
            return CachedPackets.Values.ToArray();
        }
    }
}