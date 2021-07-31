using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Json;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Serialization;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    [Serializable]
    public class MatchmakingBehaviour : INeutronMatchmaking, INeutronSerializable, ISerializationCallbackReceiver
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
        public NeutronPlayer Player { get; set; }
        /// <summary>
        ///* As propriedades do atual Matchmaking.
        /// </summary>
        public Dictionary<string, object> Get { get; }
        /// <summary>
        ///* O cache de pacotes do atual Matchmaking.
        /// </summary>
        public Dictionary<(int, int), NeutronCache> CachedPackets => _cachedPackets;
        /// <summary>
        ///* A lista de jogadores do atual Matchmaking.
        /// </summary>
        public PlayerDictionary PlayerDictionary { get => _players; set => _players = value; }
        /// <summary>
        ///* O SceneView do atual Matchmaking.
        /// </summary>
        public SceneView SceneView { get => _sceneView; set => _sceneView = value; }
        #endregion
        public MatchmakingBehaviour() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public MatchmakingBehaviour(string roomName, int maxPlayers, string properties)
        {
            Name = roomName;
            MaxPlayers = maxPlayers;
            Properties = properties;
            ////////////////////// Initialize Instances ////////////////
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties);
            SceneView = new SceneView();
            PlayerDictionary = new PlayerDictionary();
        }

        public MatchmakingBehaviour(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            PlayerCount = info.GetInt32("playerCount");
            MaxPlayers = info.GetInt32("maxPlayers");
            Properties = info.GetString("properties");
            ////////////////////// Initialize Instances ////////////////
            Get = JsonConvert.DeserializeObject<Dictionary<string, object>>(Properties);
            SceneView = new SceneView();
            _players = new PlayerDictionary();
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("playerCount", PlayerCount);
            info.AddValue("maxPlayers", MaxPlayers);
            info.AddValue("properties", Properties);
        }

        public bool Add(NeutronPlayer player)
        {
            if (PlayerCount >= MaxPlayers)
                return
                    LogHelper.Error("Failed to enter, exceeded the maximum players limit.");
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

        public NeutronPlayer[] Players(Func<NeutronPlayer, bool> predicate)
        {
            return PlayerDictionary.Values.Where(predicate).ToArray();
        }

        public NeutronCache[] Caches()
        {
            return CachedPackets.Values.ToArray();
        }

        public void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            Title = _name;
#endif
        }

        public void OnAfterDeserialize()
        {
            Title = _name;
        }
    }
}