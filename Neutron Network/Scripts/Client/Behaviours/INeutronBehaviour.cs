using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronBehaviour : MonoBehaviour
    {
        #region ID
        [Header("[Identity]")]
        [Separator] public int ID;
        #endregion

        #region Properties
        protected bool Initialized { get; set; }
        public NeutronView NeutronView { get; set; }
        #endregion

        #region Virtual Methods
        public virtual void OnNeutronStart() => Initialized = true;
        protected virtual void OnNeutronUpdate() { }
        protected virtual void OnNeutronFixedUpdate() { }
        #endregion

        #region Extended Properties
        protected bool IsMine => Initialized && Mine();
        protected bool IsBot => Initialized && NeutronView.owner.IsBot;
        protected bool IsServer => Initialized && NeutronView.isServer;
        protected bool IsClient => Initialized && !NeutronView.isServer;
        protected bool HasAuthority
        {
            get
            {
                if (NeutronView != null)
                {
                    switch (NeutronView.authorityMode)
                    {
                        case AuthorityMode.Owner:
                            return IsMine;
                        case AuthorityMode.Server:
                            return IsServer;
                        case AuthorityMode.IgnoreExceptServer:
                            return !IsServer;
                        case AuthorityMode.MasterClient:
                            return MasterClient();
                        case AuthorityMode.Ignore:
                            return true;
                        default:
                            return false;
                    }
                }
                else return NeutronUtils.LoggerError("Unable to find Neutron View");
            }
        }
        #endregion

        #region MonoBehaviour
        public void Awake()
        { }

        public void Update()
        {
            if (Initialized)
                OnNeutronUpdate();
        }

        public void FixedUpdate()
        {
            if (Initialized)
                OnNeutronFixedUpdate();
        }
        #endregion

        #region Neutron
        private bool Mine()
        {
            return !IsServer && NeutronView._.IsMine(NeutronView.owner);
        }

        private bool MasterClient()
        {
            return !IsServer && NeutronView._.IsMasterClient();
        }

        protected void Dynamic(int DynamicID, bool IsCached, NeutronWriter parameters, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            int uniqueID = DynamicID ^ ID;
            if (IsClient)
                NeutronView._.Dynamic(NeutronView.ID, uniqueID, parameters, sendTo, IsCached, broadcast, protocol);
            else if (IsServer)
                Neutron.Server.Dynamic(NeutronView.ID, uniqueID, parameters, NeutronView.owner, sendTo, IsCached, broadcast, protocol);
        }

        protected void Dynamic(int networkID, int DynamicID, bool IsCached, NeutronWriter parameters, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            int uniqueID = DynamicID ^ ID;
            if (IsClient)
                NeutronView._.Dynamic(networkID, uniqueID, parameters, sendTo, IsCached, broadcast, protocol);
            else if (IsServer)
                Neutron.Server.Dynamic(networkID, uniqueID, parameters, NeutronView.owner, sendTo, IsCached, broadcast, protocol);
        }
        #endregion
    }
}