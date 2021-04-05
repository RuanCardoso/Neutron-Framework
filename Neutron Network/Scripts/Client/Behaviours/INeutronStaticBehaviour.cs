using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    public class NeutronNonDynamicBehaviour : MonoBehaviour
    {
        #region Collections
        public static Dictionary<int, RemoteProceduralCall> NonDynamics = new Dictionary<int, RemoteProceduralCall>();
        #endregion

        #region MonoBehaviour
        private void OnEnable()
        {
            GetAttributes();
        }
        #endregion

        #region Neutron
        /// <summary>
        /// server side.
        /// </summary>
        /// <param name="nonDynamicID"></param>
        /// <param name="parameters"></param>
        /// <param name="sender"></param>
        /// <param name="cacheMode"></param>
        /// <param name="sendTo"></param>
        /// <param name="broadcast"></param>
        /// <param name="protocol"></param>
        protected void NonDynamic(int nonDynamicID, NeutronWriter parameters, Player sender, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol)
        {
            Neutron.Server.NonDynamic(sender, nonDynamicID, parameters, cacheMode, sendTo, broadcast, protocol);
        }
        /// <summary>
        /// client side.
        /// </summary>
        /// <param name="nonDynamicID"></param>
        /// <param name="parameters"></param>
        /// <param name="cacheMode"></param>
        /// <param name="sendTo"></param>
        /// <param name="broadcast"></param>
        /// <param name="protocol"></param>
        /// <param name="instance"></param>
        protected void NonDynamic(int nonDynamicID, NeutronWriter parameters, CacheMode cacheMode, SendTo sendTo, Broadcast broadcast, Protocol protocol, Neutron instance)
        {
            instance.NonDynamic(nonDynamicID, parameters, cacheMode, sendTo, broadcast, protocol);
        }
        #endregion

        #region Reflection
        private void GetAttributes()
        {
            NeutronNonDynamicBehaviour mInstance = this;
            if (mInstance != null)
            {
                var mType = mInstance.GetType();
                MethodInfo[] mInfos = mType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                for (int y = 0; y < mInfos.Length; y++)
                {
                    NonDynamic NeutronNonDynamicAttr = mInfos[y].GetCustomAttribute<NonDynamic>();
                    if (NeutronNonDynamicAttr != null)
                    {
                        RemoteProceduralCall remoteProceduralCall = new RemoteProceduralCall(mInstance, mInfos[y]);
                        NonDynamics.Add(NeutronNonDynamicAttr.ID, remoteProceduralCall);
                        if (mInfos[y].ReturnType == typeof(bool) && !NeutronConfig.Settings.GlobalSettings.SendOnPostProcessing)
                            NeutronUtils.LoggerError($"Boolean return in NonDynamic -> {remoteProceduralCall.method.Name} : [{NeutronNonDynamicAttr.ID}] is useless when \"SendOnPostProcessing\" is disabled, switch to void instead of bool");
                    }
                    else continue;
                }
            }
        }
        #endregion
    }
}