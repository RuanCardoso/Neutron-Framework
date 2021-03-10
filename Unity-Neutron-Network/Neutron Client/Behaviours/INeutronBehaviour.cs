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
        public virtual void OnNeutronStart() { }
        ///* Properties
        public NeutronView NeutronView { get; set; }
        protected bool IsMine
        {
            get
            {
                if (NeutronView != null)
                    return !IsServer && NeutronView._.isLocalPlayer(NeutronView.owner);
                else
                    return Utilities.LoggerError(ERROR_0X1);
            }
        }
        protected bool IsBot
        {
            get
            {
                if (NeutronView != null)
                    return NeutronView.owner.IsBot;
                else
                    return Utilities.LoggerError(ERROR_0X1);
            }
        }
        protected bool IsServer
        {
            get
            {
                if (NeutronView != null)
                    return NeutronView.isServerOrClient;
                else
                    return Utilities.LoggerError(ERROR_0X1);
            }
        }
        protected bool IsClient
        {
            get
            {
                if (NeutronView != null)
                    return !NeutronView.isServerOrClient;
                else
                    return Utilities.LoggerError(ERROR_0X1);
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
            int currentFPS = NeutronConfig.GetConfig.serverFPS;
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