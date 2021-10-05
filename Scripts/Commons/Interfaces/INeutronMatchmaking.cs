using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Server.Internal;
using Newtonsoft.Json.Linq;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronMatchmaking
    {
        #region Properties
        string Name { get; set; }
        int PlayerCount { get; }
        int MaxPlayers { get; set; }
        string Properties { get; set; }
        NeutronPlayer Owner { get; set; }
        NeutronSafeDictionary<(int, int, RegisterMode), NeutronView> Views { get; }
        JObject Get { get; }
        PhysicsManager PhysicsManager { get; set; }
        #endregion

        #region Methods
        bool Add(NeutronPlayer player);
        bool Remove(NeutronPlayer player);
        void Add(NeutronCache cache, int viewId);
        void Apply(NeutronRoom room);
        void Apply(NeutronChannel channel);
        void Apply(INeutronMatchmaking matchmaking);
        void Clear();
        NeutronPlayer[] Players();
        NeutronCache[] Caches();
        #endregion
    }
}