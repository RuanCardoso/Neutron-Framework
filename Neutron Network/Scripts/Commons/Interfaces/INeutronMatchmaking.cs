using System.Collections.Generic;
using NeutronNetwork;

public interface INeutronMatchmaking
{
    #region Primitives
    string Name { get; set; }
    int CountOfPlayers { get; set; }
    int MaxPlayers { get; set; }
    string _ { get; set; }
    #endregion

    #region Classes/Struct
    Player Owner { get; set; }
    SceneSettings SceneSettings { get; set; }
    #endregion

    #region Collections
    Dictionary<string, object> Get { get; set; }
    #endregion

    #region Methods
    bool AddPlayer(Player player);
    bool RemovePlayer(Player player);
    void AddCache(CachedBuffer buffer);
    Player[] GetPlayers();
    CachedBuffer[] GetCaches();
    #endregion
}