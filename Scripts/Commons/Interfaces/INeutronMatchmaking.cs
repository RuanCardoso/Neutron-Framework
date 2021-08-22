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
        NeutronPlayer Player { get; set; }
        SceneView SceneView { get; }
        JObject Get { get; }
        #endregion

        #region Methods
        bool Add(NeutronPlayer player);
        bool Remove(NeutronPlayer player);
        void Add(NeutronCache cache, int viewId);
        NeutronPlayer[] Players();
        NeutronCache[] Caches();
        #endregion
    }
}