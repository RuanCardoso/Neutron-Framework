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
        public virtual void OnNeutronStart() { Initialized = true; }
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
                        if (NeutronView.authorityMode == AuthorityMode.Owner)
                        {
                            if (NeutronView._ != null)
                            {
                                return !IsServer && NeutronView._.isLocalPlayer(NeutronView.owner);
                            }
                            else return false;
                        }
                        else if (NeutronView.authorityMode == AuthorityMode.Server)
                        {
#if UNITY_SERVER
                        return IsServer;
#elif UNITY_EDITOR
                            return true;
#else
                        return false;
#endif
                        }
                        else if (NeutronView.authorityMode == AuthorityMode.IgnoreExceptServer)
                        {
#if UNITY_SERVER
                        return false;
#else
                            return true;
#endif
                        }
                        else if (NeutronView.authorityMode == AuthorityMode.MasterClient)
                        {
                            return true;
                        }
                        else if (NeutronView.authorityMode == AuthorityMode.Ignore)
                        {
                            return true;
                        }
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
        ///* Reflection
        public MethodInfo[] methods { get; private set; }

        public void Awake()
        {
            GetMethods();
        }

        private void GetMethods()
        {
            methods = this.GetType().GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
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