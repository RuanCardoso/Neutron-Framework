using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    public class MatchmakingBehaviour : INeutronMatchmaking, ISerializationCallbackReceiver
    {
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052

        #region Fields
        [SerializeField] [ReadOnly] [AllowNesting] protected int _id;
        [SerializeField] private string _name;
        [SerializeField] [ReadOnly] [AllowNesting] private int _playerCount; // Only show in inspector.
        [SerializeField] private int _maxPlayers; // Thread safe. Immutable
        [SerializeField] [ResizableTextArea] private string _properties = "{\"Neutron\":\"Neutron\"}";
        [SerializeField] [HorizontalLine] private PlayerDictionary _players;
        [SerializeField] private SceneView _sceneView;
        #endregion

        #region Fields -> Not Serialized
        private readonly Dictionary<(int, int), NeutronCache> _cachedPackets = new Dictionary<(int, int), NeutronCache>();
        private int _cacheId;
        #endregion

        #region Properties
        /// <summary>
        ///* Define o nome do atual Matchmaking.
        /// </summary>
        public string Name { get => _name; set => _name = value; }
        /// <summary>
        ///* Retorna a quantidade atual de jogadores do atual Matchmaking.
        /// </summary>
        public int PlayerCount { get => _playerCount; set => _playerCount = value; }
        /// <summary>
        ///* Define a quantidade máxima de jogadores do atual Matchmaking.
        /// </summary>
        public int MaxPlayers { get => _maxPlayers; set => _maxPlayers = value; }
        /// <summary>
        ///* Define as propridades do atual Matchmaking.
        /// </summary>
        public string Properties { get => _properties; set => _properties = value; }
        /// <summary>
        ///* O jogador dono do atual Matchmaking.
        /// </summary>
        public NeutronPlayer Owner { get; set; }
        /// <summary>
        ///* As propriedades do atual Matchmaking.
        /// </summary>
        public Dictionary<string, object> Get { get; set; }
        /// <summary>
        ///* O cache de pacotes do atual Matchmaking.
        /// </summary>
        private Dictionary<(int, int), NeutronCache> CachedPackets => _cachedPackets;
        /// <summary>
        ///* A lista de jogadores do atual Matchmaking.
        /// </summary>
        private PlayerDictionary PlayerDictionary => _players;
        /// <summary>
        ///* O SceneView do atual Matchmaking.
        /// </summary>
        public SceneView SceneView { get => _sceneView; set => _sceneView = value; }
        #endregion

        public bool Add(NeutronPlayer player)
        {
            if (PlayerCount >= MaxPlayers)
                return LogHelper.Error("Matchmaking: failed to enter, exceeded the maximum players limit.");
            else
            {
                bool TryValue;
                if ((TryValue = PlayerDictionary.TryAdd(player.ID, player)))
                    PlayerCount++;
                return TryValue;
            }
        }

        public void Add(NeutronCache neutronCache)
        {
            int id = neutronCache.Owner.ID;
            switch (neutronCache.Cache)
            {
                case Cache.Overwrite:
                    {
                        (int, int) key = (id, neutronCache.Id);
                        if (CachedPackets.ContainsKey(key))
                            CachedPackets[key] = neutronCache;
                        else
                            CachedPackets.Add(key, neutronCache);
                    }
                    break;
                case Cache.New:
                    {
                        (int, int) key = (id, ++_cacheId);
                        CachedPackets.Add(key, neutronCache);
                    }
                    break;
            }
        }

        public bool Remove(NeutronPlayer player)
        {
            bool TryValue;
            if ((TryValue = PlayerDictionary.TryRemove(player.ID, out NeutronPlayer _)))
            {
                var cachedPackets = CachedPackets.Where(x => x.Value.Owner.Equals(player)).ToList();
                foreach (var neutronCache in cachedPackets)
                    CachedPackets.Remove(neutronCache.Key);
                PlayerCount--;
            }
            return TryValue;
        }

        public NeutronPlayer[] Players()
        {
            return PlayerDictionary.Values.ToArray();
        }

        public NeutronCache[] Caches()
        {
            return CachedPackets.Values.ToArray();
        }

        public void OnBeforeSerialize()
        {

        }

        public void OnAfterDeserialize()
        {
            Title = _name;
        }
    }
}