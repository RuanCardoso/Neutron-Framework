using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using NeutronNetwork.Attributes;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

namespace NeutronNetwork.Internal
{
    /// <summary>
    /// This class is the basis of all network objects.
    /// </summary>
    public class NeutronViewBehaviour : MonoBehaviour
    {
        #region Primitives
        /// <summary>
        /// This ID is used to identify the instance.
        /// </summary>
        [ID] public int ID;
        /// <summary>
        /// This field defines whether the object should be created on the server, client, or both.
        /// </summary>
        public Ambient Ambient = Ambient.Both;
        /// <summary>
        /// Gets a value that indicates whether the object is from the server or not.
        /// </summary>
        [ReadOnly] public bool IsServer;
        #endregion

        #region Properties
        /// <summary>
        /// Gets a value that indicates whether the object is a player or a scene object.
        /// </summary>
        public bool IsSceneObject => SceneHelper.IsSceneObject(ID);
        #endregion

        #region Register
        /// <summary>
        /// Gets the instance owner.
        /// </summary>
        [ReadOnly] public Player Owner;
        /// <summary>
        /// The network instance of the object.
        /// </summary>
        [NonSerialized] public Neutron _;
        #endregion

        #region Collection
        /// <summary>
        /// Here is stored all the methods marked with the attribute "iRPC"
        /// </summary>
        [NonSerialized] public Dictionary<int, RemoteProceduralCall> Dynamics = new Dictionary<int, RemoteProceduralCall>();
        #endregion

        #region Object Properties
        /// <summary>
        /// This variable is used to synchronize the current position of the object in all clients who join later.
        /// </summary>
        public Vector3 LastPosition { get; set; }
        /// <summary>
        /// This variable is used to synchronize the current rotation of the object in all clients who join later.
        /// </summary>
        public Vector3 LastRotation { get; set; }
        #endregion

        #region MonoBehaviour
        public void Awake() => GetNonDynamicAttributes();

        public void Update()
        {
            LastPosition = transform.position;
            LastRotation = transform.eulerAngles;
        }
        #endregion

        #region Virtual Methods
        /// <summary>
        /// This method is called when an object is instantiated and is ready for use on the network.
        /// </summary>
        public virtual void OnNeutronStart() { }
        /// <summary>
        /// This method is called when an object is instantiated.
        /// </summary>
        public virtual void OnNeutronAwake() { }
        #endregion

        #region Editor
        private void OnValidate()
        {
#if UNITY_EDITOR
            if (gameObject.activeInHierarchy)
            {
                foreach (Transform tr in transform)
                {
                    if (tr.TryGetComponent<NeutronView>(out NeutronView _))
                        NeutronLogger.Print("Child objects cannot have \"NeutronView\", because their parent already has one.");
                }
            }
#endif
        }
        #endregion

        #region Reflection
        /// <summary>
        /// This method stores all methods that are marked with the attribute "iRPC"
        /// This method uses reflection, it is called only once when the object is instantiated.
        /// </summary>
        private void GetNonDynamicAttributes()
        {
            var neutronBehaviours = GetComponentsInChildren<NeutronBehaviour>();
            if (neutronBehaviours != null)
            {
                for (int i = 0; i < neutronBehaviours.Length; i++)
                {
                    NeutronBehaviour mInstance = neutronBehaviours[i];
                    if (mInstance != null)
                    {
                        var mType = mInstance.GetType();
                        MethodInfo[] mInfos = mType.GetMethods(BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance);
                        for (int y = 0; y < mInfos.Length; y++)
                        {
                            iRPC[] Attrs = mInfos[y].GetCustomAttributes<iRPC>().ToArray();
                            if (Attrs != null)
                            {
                                foreach (iRPC Attr in Attrs)
                                {
                                    int uniqueID = Attr.ID ^ mInstance.ID;
                                    if (!Dynamics.ContainsKey(uniqueID))
                                        Dynamics.Add(uniqueID, new RemoteProceduralCall(mInstance, mInfos[y], Attr));
                                    else NeutronLogger.Print($"Duplicate ID not allowed in \"{mInstance.GetType().Name}\".");
                                }
                            }
                            else continue;
                        }
                    }
                    else continue;
                }
            }
            else NeutronLogger.Print("Could not find any implementation of \"NeutronBehaviour\"");
        }
        #endregion
    }
}