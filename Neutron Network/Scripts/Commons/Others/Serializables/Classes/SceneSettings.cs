using System;
using System.Collections.Generic;
using NeutronNetwork;
using NeutronNetwork.Attributes;
using UnityEngine;

namespace NeutronNetwork
{
    [Serializable]
    public class SceneSettings
    {
        public Dictionary<int, NeutronView> networkObjects = new Dictionary<int, NeutronView>();
        public GameObject[] sceneObjects;
        [Separator] public bool enablePhysics;
    }
}