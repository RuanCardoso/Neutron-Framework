using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronBehaviour : MonoBehaviour
    {
        [NonSerialized] public NeutronView NeutronView;
        public bool IsMine {
            get => !IsServer && NeutronView._.isLocalPlayer(NeutronView.owner);
        }
        public bool IsBot { get => NeutronView.owner.isBot; }
        public bool IsServer { get => NeutronView.isServerOrClient; }
        public bool IsClient { get => !NeutronView.isServerOrClient; }
    }
}