using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Interfaces;
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
#pragma warning disable IDE0052
        [SerializeField] [HideInInspector] private string Title = "Neutron";
#pragma warning restore IDE0052

        #region Fields
        [ReadOnly]
        [AllowNesting]
        [SerializeField] protected int _id = 0;
        [SerializeField] private string _name = "Neutron";
        [ReadOnly]
        [AllowNesting]
        [SerializeField] private int _playerCount = 0;
        [SerializeField] private int _maxPlayers = 5;
        [ResizableTextArea]
        [SerializeField] private string _properties = "{\"Neutron\":\"Neutron\"}";
        [HorizontalLine]
        [SerializeField] private PlayerDictionary _players = new PlayerDictionary();
        [SerializeField] private SceneView _sceneView = new SceneView();
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
            set => _properties = value;
        }

        /// <summary>
        ///* O jogador dono do atual Matchmaking.
        /// </summary>
        public NeutronPlayer Player {
            get;
            set;
        }

        /// <summary>
        ///* O SceneView do atual Matchmaking.
        /// </summary>
        public SceneView SceneView {
            get => _sceneView;
        }

        /// <summary>
        ///* As propriedades do atual Matchmaking.
        /// </summary>
        public JObject Get {
            get;
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
        #endregion

        public MatchmakingBehaviour() { } //* the default constructor is important for deserialization and serialization.(only if you implement the ISerializable interface or JSON.Net).

        public MatchmakingBehaviour(string roomName, int maxPlayers, string properties)
        {
            Name = roomName;
            MaxPlayers = maxPlayers;
            Properties = properties;
            //*********************************************
            Get = JObject.Parse(Properties);
        }

        public MatchmakingBehaviour(SerializationInfo info, StreamingContext context)
        {
            Name = info.GetString("name");
            MaxPlayers = info.GetInt32("maxPlayers");
            Properties = info.GetString("properties");
            //*********************************************
            _playerCount = info.GetInt32("playerCount");
            //*********************************************
            Get = JObject.Parse(Properties);
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
                        (int, int, int) key = (neutronCache.Owner.ID, neutronCache.Id, viewId);
                        if (CachedPackets.ContainsKey(key))
                            CachedPackets[key] = neutronCache;
                        else
                            CachedPackets.Add(key, neutronCache);
                    }
                    break;
                case CacheMode.New:
                    {
                        (int, int, int) key = (neutronCache.Owner.ID, ++_cacheId, viewId);
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
                _playerCount--;
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
#endif
        }
    }
}