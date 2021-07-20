using NeutronNetwork.Server.Internal;
using System.Collections.Generic;

namespace NeutronNetwork.Internal.Interfaces
{
    public interface INeutronMatchmaking
    {
        #region Primitives
        string Name { get; set; }
        int CountOfPlayers { get; set; }
        int MaxPlayers { get; set; }
        string _ { get; set; }
        #endregion

        #region Classes/Struct
        NeutronPlayer Owner { get; set; }
        SceneView SceneSettings { get; set; }
        #endregion

        #region Collections
        Dictionary<string, object> Get { get; set; }
        #endregion

        #region Methods
        bool AddPlayer(NeutronPlayer player);
        bool RemovePlayer(NeutronPlayer player);
        void AddCache(NeutronCache buffer);
        NeutronPlayer[] GetPlayers();
        NeutronCache[] GetCaches();
        #endregion
    }
}