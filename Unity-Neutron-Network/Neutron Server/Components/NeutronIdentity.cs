using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronIdentity : MonoBehaviour
    {
        public bool ServerOnly = false;
        public Identity Identity;
    }

    [Serializable]
    public class Identity
    {
        public int ownerID;
        public int channelID;
        public int roomID;
        public int objectID;
    }
}