using System;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronBehaviour : MonoBehaviour
    {
        ///* error messages.
        private const string ERROR_0X1 = "It was not possible to instantiate this object on a network";
        ///* Virtual Methods
        public virtual void OnNeutronStart() => Initialized = true;
        public virtual void OnNeutronUpdate() { }
        public virtual void OnNeutronFixedUpdate() { }
        ///* Properties
        protected bool Initialized { get; set; }
        public NeutronView NeutronView { get; set; }
        protected bool IsMine
        {
            get
            {
                if (Initialized)
                {
                    if (NeutronView != null)
                    {
                        return Mine();
                    }
                    else return NeutronUtils.LoggerError(ERROR_0X1);
                }
                else return false;
            }
        }
        protected bool HasAuthority
        {
            get
            {
                if (Initialized)
                {
                    if (NeutronView != null)
                    {
                        if (NeutronView.authorityMode == AuthorityMode.Owner)
                            return Mine();
                        else if (NeutronView.authorityMode == AuthorityMode.Server)
                            return IsServer;
                        else if (NeutronView.authorityMode == AuthorityMode.IgnoreExceptServer)
                            return !IsServer;
                        else if (NeutronView.authorityMode == AuthorityMode.MasterClient)
                            return !IsServer && NeutronView._ != null && NeutronView._.IsMasterClient();
                        else if (NeutronView.authorityMode == AuthorityMode.Ignore)
                            return true;
                        else return false;
                    }
                    else return NeutronUtils.LoggerError(ERROR_0X1);
                }
                else return false;
            }
        }
        protected bool IsBot
        {
            get
            {
                if (Initialized)
                {
                    if (NeutronView != null)
                        return NeutronView.owner.IsBot;
                    else
                        return NeutronUtils.LoggerError(ERROR_0X1);
                }
                else return false;
            }
        }
        protected bool IsServer
        {
            get
            {
                if (Initialized)
                {
                    if (NeutronView != null)
                        return NeutronView.isServer;
                    else
                        return NeutronUtils.LoggerError(ERROR_0X1);
                }
                else return false;
            }
        }
        protected bool IsClient
        {
            get
            {
                if (Initialized)
                {
                    if (NeutronView != null)
                        return !NeutronView.isServer;
                    else
                        return NeutronUtils.LoggerError(ERROR_0X1);
                }
                else return false;
            }
        }

        public void Awake()
        {

        }

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

        private bool Mine()
        {
            return !IsServer && NeutronView._ != null && NeutronView._.IsMine(NeutronView.owner);
        }

        protected void Dynamic(int DynamicID, NeutronWriter parameters, SendTo sendTo, bool Cached, Broadcast broadcast, Protocol protocol)
        {
            if (IsClient)
                NeutronView._.Dynamic(NeutronView, DynamicID, parameters, sendTo, Cached, broadcast, protocol);
            else if (IsServer)
                Neutron.Server.Dynamic(NeutronView, DynamicID, parameters, sendTo, Cached, broadcast, protocol);
        }

        protected int GetMaxPacketsPerSecond(float sInterval)
        {
#if UNITY_SERVER || UNITY_EDITOR
            int currentFPS = NeutronConfig.Settings.ServerSettings.FPS;
            if (sInterval == 0) return currentFPS;
            float interval = (sInterval * currentFPS);
            float MPPS = currentFPS / interval;
            return (int)MPPS;
#else
            return 0;
#endif
        }
    }
}