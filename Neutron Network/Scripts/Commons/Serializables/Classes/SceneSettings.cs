using System;
using System.Collections.Generic;
using NeutronNetwork;
using UnityEngine;

[Serializable]
public class SceneSettings
{
    public Dictionary<int, NeutronView> networkObjects = new Dictionary<int, NeutronView>();
    public GameObject[] sceneObjects;
    public bool enablePhysics;
    public bool clientOnly = true;
}