using System;
using UnityEngine;

namespace NeutronNetwork.Client.Internal
{
    [Serializable]
    public class NeutronComponent
    {
        public Component component;
        public GameObject gameObject;
        public ComponentMode componentMode;
    }
}