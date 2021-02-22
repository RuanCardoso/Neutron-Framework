using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronBehaviour : MonoBehaviour
    {
        public virtual void OnNeutronStart() { }
        public NeutronView NeutronView { get; set; }
        protected bool IsMine
        {
            get => !IsServer && NeutronView._.isLocalPlayer(NeutronView.owner);
        }
        protected bool IsBot { get => NeutronView.owner.isBot; }
        protected bool IsServer { get => NeutronView.isServerOrClient; }
        protected bool IsClient { get => !NeutronView.isServerOrClient; }
    }
}