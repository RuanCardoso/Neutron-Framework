using NeutronNetwork.Helpers;
using NeutronNetwork.Extensions;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
#if UNITY_EDITOR
#endif

//* Created by: Ruan Cardoso(Brasil)
//* Email: neutron050322@gmail.com
//* License: MIT
namespace NeutronNetwork.Internal
{
    /// <summary>
    ///* This class provides an identity to the object on the network.
    /// </summary>
    public class ViewBehaviour : MonoBehaviour
    {
        #region Fields
        /// <summary>
        ///* The unique identity of the object on the network.
        /// </summary>
        [SerializeField] private int _id;
        /// <summary>
        ///* On which side should the object exist?
        /// </summary>
        [SerializeField]
        private Side _side = Side.Both;
        /// <summary>
        ///* Defines if the object has auto destruction.
        /// </summary>
        [SerializeField]
        private bool _autoDestroy = true;
        /// <summary>
        ///* Returns the owner of the object.
        /// </summary>
        [SerializeField]
        [InfoBox("The properties of the owner of this network object.")]
        private NeutronPlayer _owner;
        #endregion

        #region Properties
        /// <summary>
        ///* Returns the unique identity of the object on the network.
        /// </summary>
        /// <value></value>
        public int Id
        {
            get => _id;
            set => _id = value;
        }

        /// <summary>
        ///* Returns the instance owns this object.
        /// </summary>
        /// <value></value>
        public Neutron This
        {
            get;
            protected set;
        }

        /// <summary>
        ///* Returns the owner of the object.
        /// </summary>
        /// <value></value>
        public NeutronPlayer Owner
        {
            get => _owner;
            protected set => _owner = value;
        }

        /// <summary>
        ///* Returns if object is server-side.
        /// </summary>
        /// <value></value>
        public bool IsServer
        {
            get;
            protected set;
        }

        /// <summary>
        ///* Returns if object has auto destruction.
        /// </summary>
        /// <value></value>
        public bool AutoDestroy
        {
            get => _autoDestroy;
            set => _autoDestroy = value;
        }

        /// <summary>
        ///* Returns the side of the object.
        /// </summary>
        public Side Side => _side;

        /// <summary>
        ///* Returns the is a scene object.
        /// </summary>
        public bool IsSceneObject => RegisterMode == RegisterMode.Scene;

        /// <summary>
        ///* Defines the register mode.
        /// </summary>
        /// <value></value>
        public RegisterMode RegisterMode
        {
            get;
            set;
        }

        public Transform Transform
        {
            get;
            set;
        }
        #endregion

        #region Collections
        /// <summary>
        ///* Store all iRPC methods of all instances.
        /// </summary>
        [NonSerialized] public Dictionary<(byte, byte), RPCInvoker> iRPCs = new Dictionary<(byte, byte), RPCInvoker>();
        /// <summary>
        ///* Store all network instances....
        [NonSerialized] public Dictionary<int, NeutronBehaviour> NeutronBehaviours = new Dictionary<int, NeutronBehaviour>();
        #endregion

        #region Mono Behaviour
        private void Start() => Transform = transform;

        //* Impede que objetos filhos tenham objeto de rede, caso o pai já tenha um.
        private void OnEnable()
        {
#if UNITY_EDITOR
            if (gameObject.activeInHierarchy)
            {
                foreach (Transform tr in transform)
                {
                    //* Check if the object has a network object.
                    if (tr.TryGetComponent<NeutronView>(out NeutronView _))
                    {
                        if (!LogHelper.Error("Child objects cannot have \"NeutronView\", because their parent already has one."))
                            Destroy(gameObject);
                    }
                    else
                        continue; //* Continue if the object has no network object.
                }
            }
#endif
        }

        private void Reset()
        {
#if UNITY_EDITOR
            Id = 0; //* Set the default value.
            OnValidate(); //* Validate the object on reset.
#endif
        }

        private void OnValidate()
        {
#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                if (SceneHelper.IsInScene(gameObject))
                {
                    //* Check if the object is in the scene.
                    var views = FindObjectsOfType<NeutronView>(); //* Find all network objects in the scene.
                    if (Id == 0)
                        Id = Helper.GetAvailableId(views, x => x.Id, short.MaxValue); //* Get the available id.
                    else
                    {
                        if (!(Id >= short.MaxValue))
                        {
                            //* Check if the id is valid.
                            int count = views.Count(x => x.Id == Id); //* find duplicated id.
                            if (count > 1)
                                Reset(); //* Reset the id if it is already in use.
                        }
                        else
                            LogHelper.Error("Max Neutron Views reached!");
                    }
                }
                else
                    Id = 0; //* Set the default value.
            }
#endif
        }
        #endregion

        #region Virtual Methods
        public virtual void OnNeutronStart() { }
        public virtual void OnNeutronAwake() { }
        public virtual bool OnNeutronRegister(NeutronPlayer player, bool isServer, RegisterMode registerMode, Neutron instance) => true;
        #endregion

        #region Reflection
        public void MakeAttributes()
        {
            var childs = GetComponentsInChildren<NeutronBehaviour>(); //* Get all network instances in the scene.
            if (childs.Length > 0)
            {
                for (int c = 0; c < childs.Length; c++)
                {
                    NeutronBehaviour child = childs[c]; //* Get the current network instance.
                    #region Add Instances
                    if (!NeutronBehaviours.ContainsKey(child.Id)) //* Check if the instance is already in the dictionary.
                        NeutronBehaviours.Add(child.Id, child); //* Add the instance to the dictionary.
                    else
                        LogHelper.Error($"Duplicate \"NeutronBehaviour\" ID not allowed in \"{child.GetType().Name}\". {child.Id}");
                    #endregion

                    if (child != null && child.enabled)
                    {
                        //* Check if the instance is enabled.
                        (iRPCAttribute[], MethodInfo)[] multiplesMethods = ReflectionHelper.GetMultipleAttributesWithMethod<iRPCAttribute>(child); //* Get all iRPC methods.
                        for (int i = 0; i < multiplesMethods.Length; i++)
                        {
                            (iRPCAttribute[], MethodInfo) methods = multiplesMethods[i]; //* Get the current iRPC method.
                            for (int ii = 0; ii < methods.Item1.Count(); ii++)
                            {
                                iRPCAttribute method = methods.Item1[ii]; //* Get the current iRPC attribute.
                                (byte, byte) key = (method.Id, child.Id); //* Get the key.
                                if (!iRPCs.ContainsKey(key)) //* Check if the key is already in the dictionary.
                                    iRPCs.Add(key, new RPCInvoker(child, methods.Item2, method)); //* Add the key to the dictionary.
                                else
                                    LogHelper.Error($"Duplicate ID not allowed in \"{child.GetType().Name}\".");
                            }
                        }
                    }
                }
            }
        }
        #endregion
    }
}