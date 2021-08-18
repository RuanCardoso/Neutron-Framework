using NeutronNetwork.Server.Internal;
using System.Collections.Generic;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronMatchmaking
    {
        #region Properties
        string Name { get; set; }
        int PlayerCount { get; set; }
        int MaxPlayers { get; set; }
        string Properties { get; set; }
        NeutronPlayer Player { get; set; }
        SceneView SceneView { get; }
        Dictionary<string, object> Get { get; }
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