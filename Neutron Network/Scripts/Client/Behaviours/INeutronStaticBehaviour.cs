using NeutronNetwork.Constants;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;

namespace NeutronNetwork
{
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_CONNECTION)]
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
        protected void sRPC(int nonDynamicID, NeutronWriter parameters, Player sender)
        {
            Neutron.Server.sRPC(sender, nonDynamicID, parameters);
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
        protected void sRPC(int nonDynamicID, NeutronWriter parameters, Protocol protocol, Neutron instance)
        {
            instance.sRPC(instance.MyPlayer.ID, nonDynamicID, parameters, protocol);
        }

        protected void sRPC(Player nID, int nonDynamicID, NeutronWriter parameters, Protocol protocol, Neutron instance)
        {
            instance.sRPC(nID.ID, nonDynamicID, parameters, protocol);
        }

        protected void sRPC(NeutronView nID, int nonDynamicID, NeutronWriter parameters, Protocol protocol, Neutron instance)
        {
            instance.sRPC(nID.ID, nonDynamicID, parameters, protocol);
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
                    sRPC[] Attrs = mInfos[y].GetCustomAttributes<sRPC>().ToArray();
                    if (Attrs != null)
                    {
                        foreach (sRPC Attr in Attrs)
                        {
                            NonDynamics.Add(Attr.ID, new RemoteProceduralCall(mInstance, mInfos[y], Attr));
                        }
                    }
                    else continue;
                }
            }
        }
        #endregion
    }
}