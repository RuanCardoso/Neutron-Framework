using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Packets;
using NeutronNetwork.Server.Internal;
using Newtonsoft.Json.Linq;
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
        private int _cacheId;
        [SerializeField]
        [HideInInspector]
        private bool _isInitialized;
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052

        #region Default Values
        private const string DEFAULT_NAME = "Neutron";
        private const int DEFAULT_MAX_PLAYERS = 15;
        private const string DEFAULT_PROPERTIES = "{\"Map\":\"Neutron\"}";
        #endregion

        #region Fields
        [ReadOnly]
        [AllowNesting]
        [SerializeField] protected int _id;
        [SerializeField] private string _name = DEFAULT_NAME;
        [ReadOnly]
        [AllowNesting]
        [SerializeField] private int _playerCount;
        [SerializeField] private int _maxPlayers = DEFAULT_MAX_PLAYERS;
        [ResizableTextArea]
        [SerializeField] private string _properties = DEFAULT_PROPERTIES;
        [HorizontalLine]
        [SerializeField] private PlayerDictionary _players = new PlayerDictionary();
        #endregion

        #region Properties
        /// <summary>
        ///* Define o nome do atual Matchmaking.
        /// </summary>
        [Network("Serialized")]
        public string Name {
            get => _name;
            set => _name = value;
        }

        /// <summary>
        ///* Retorna a quantidade atual de jogadores do atual Matchmaking.
        /// </summary>
        [Network("Serialized")]
        public int PlayerCount {
            get => _playerCount;
        }

        /// <summary>
        ///* Define a quantidade máxima de jogadores do atual Matchmaking.
        /// </summary>
        [Network("Serialized")]
        public int MaxPlayers {
            get => _maxPlayers;
            set => _maxPlayers = value;
        }

        /// <summary>
        ///* Define as propridades do atual Matchmaking.
        /// </summary>
        [Network("Serialized")]
        public string Properties {
            get => _properties;
            set {
                _properties = value;
                try
                {
                    if (!string.IsNullOrEmpty(value))
                        Get = JObject.Parse(value);
                }
                catch
                {
                    LogHelper.Error("Invalid json in properties.");
                }
            }
        }

        /// <summary>
        ///* O jogador dono do atual Matchmaking.
        /// </summary>
        public NeutronPlayer Owner {
            get;
            set;
        }

        /// <summary>
        ///* Armazena os objetos de rede do matchmaking.
        /// </summary>
        public NeutronSafeDictionary<(int, int, RegisterMode), NeutronView> Views {
            get;
        } = new NeutronSafeDictionary<(int, int, RegisterMode), NeutronView>();

        /// <summary>
        ///* As propriedades do atual Matchmaking.
        /// </summary>
        public JObject Get {
            get;
            private set;
        } = new JObject();

        /// <summary>
        ///* O cache de pacotes do atual Matchmaking.
        /// </summary>
        private Dictionary<(int, int, int), NeutronCache> CachedPackets {
            get;
        } = new Dictionary<(int, int, int), NeutronCache>();

        /// <summary>
        ///* A lista de jogadores do atual Matchmaking.
        /// </summary>
        private PlayerDictionary PlayerDictionary {
            get => _players;
        }

        /// <summary>
        ///* Obtém o gerenciador de física do Matchmaking.
        /// </summary>
        public PhysicsManager PhysicsManager {
            get;
            set;
        }
        #endregion

        public MatchmakingBehaviour() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public MatchmakingBehaviour(string roomName, int maxPlayers, string properties)
        {
            Name = roomName;
            MaxPlayers = maxPlayers;
            Properties = properties;
        }

        public MatchmakingBehaviour(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            _playerCount = info.GetInt32("playerCount");
            MaxPlayers = info.GetInt32("maxPlayers");
            Properties = info.GetString("properties");
            Owner = (NeutronPlayer)info.GetValue("owner", typeof(NeutronPlayer));
        }

        public virtual void GetObjectData(SerializationInfo info, StreamingContext context)
        {
            info.AddValue("name", Name);
            info.AddValue("playerCount", PlayerCount);
            info.AddValue("maxPlayers", MaxPlayers);
            info.AddValue("properties", Properties);
            info.AddValue("owner", Owner);
        }

        public bool Add(NeutronPlayer player)
        {
            if (PlayerCount >= MaxPlayers)
                return
                    LogHelper.Error("Failed to enter, exceeded the maximum players limit.");
            else
            {
                bool TryValue;
                if ((TryValue = PlayerDictionary.TryAdd(player.Id, player)))
                    _playerCount++;
                return TryValue;
            }
        }

        public void Add(NeutronCache neutronCache, int viewId)
        {
            switch (neutronCache.CacheMode)
            {
                case CacheMode.Overwrite:
                    {
                        (int, int, int) key = (neutronCache.Owner.Id, neutronCache.Id, viewId);
                        if (CachedPackets.ContainsKey(key))
                            CachedPackets[key] = neutronCache;
                        else
                            CachedPackets.Add(key, neutronCache);
                    }
                    break;
                case CacheMode.New:
                    {
                        (int, int, int) key = (neutronCache.Owner.Id, ++_cacheId, viewId);
                        CachedPackets.Add(key, neutronCache);
                    }
                    break;
            }
        }

        public bool Remove(NeutronPlayer player)
        {
            bool TryValue;
            if ((TryValue = PlayerDictionary.TryRemove(player.Id, out NeutronPlayer _)))
            {
                var cachedPackets = CachedPackets.Where(x => x.Value.Owner.Equals(player)).ToList();
                foreach (var neutronCache in cachedPackets)
                    CachedPackets.Remove(neutronCache.Key);
                _playerCount--;
            }
            return TryValue;
        }

        public virtual void Apply(NeutronRoom room)
        {
            Apply((INeutronMatchmaking)room);
        }

        public virtual void Apply(NeutronChannel channel)
        {
            Apply((INeutronMatchmaking)channel);
        }

        public void Apply(INeutronMatchmaking matchmaking)
        {
            _name = matchmaking.Name;
            _playerCount = matchmaking.PlayerCount;
            _maxPlayers = matchmaking.MaxPlayers;
            _properties = matchmaking.Properties;
            Owner = matchmaking.Owner;
        }

        /// <summary>
        ///* Reseta o estado do Matchmaking.
        /// </summary>
        public void Clear()
        {
            //* Limpa todo o cache do matchmaking.
            CachedPackets.Clear();
            //* Destroí todos os objetos de rede do matchmaking.
            foreach (NeutronView view in Views.Values.ToArray())
                view.Destroy();
            Views.Clear();
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

        public virtual void OnBeforeSerialize()
        {
#if UNITY_EDITOR
            Title = _name;
#endif
        }

        public virtual void OnAfterDeserialize()
        {
#if UNITY_EDITOR
            Title = _name;
            if (!_isInitialized)
            {
                _name = DEFAULT_NAME;
                _maxPlayers = DEFAULT_MAX_PLAYERS;
                _properties = DEFAULT_PROPERTIES;
                _isInitialized = true;
            }
#endif
        }
    }
}