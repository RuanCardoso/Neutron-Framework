using NeutronNetwork.Packets;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace NeutronNetwork.Editor
{
    [Serializable]
    public class SubScene
    {
        [SerializeField] public string _name;
        [SerializeField] public MatchmakingMode matchmakingMode;
        [SerializeField] public List<SubScene> _subScenes;
    }

    [Serializable]
    public class SubSceneRoom
    {
        [SerializeField] public string _name;
        [SerializeField] public MatchmakingMode matchmakingMode;
    }
}