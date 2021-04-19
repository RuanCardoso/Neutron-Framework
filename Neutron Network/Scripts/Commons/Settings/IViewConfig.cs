using System;
using System.Collections.Generic;
using System.Reflection;
using NeutronNetwork;
using NeutronNetwork.Internal;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Internal.Extesions;
using UnityEngine;

public class ViewConfig : MonoBehaviour
{
    #region Primitives
    [ID] [DisableField] public int ID;
    public Ambient ambient = Ambient.Both;
    [ReadOnly] public bool isServer;
    #endregion

    #region Properties
    public bool IsSceneObject => InternalUtils.IsSceneObject(ID);
    #endregion

    #region Register
    [ReadOnly] public Player owner;
    [NonSerialized] public Neutron _;
    #endregion

    #region Collection
    public Dictionary<int, RemoteProceduralCall> Dynamics = new Dictionary<int, RemoteProceduralCall>();
    #endregion

    #region UnityEngine
    public Vector3 lastPosition { get; set; }
    public Vector3 lastRotation { get; set; }
    #endregion

    public void Awake() => GetAttributes();

    public void Start() { }

    public virtual void OnNeutronStart()
    { }

    public virtual void OnNeutronAwake()
    { }

    public void Update()
    {
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
    }

    private void OnValidate()
    {
        if (gameObject.activeInHierarchy)
        {
            foreach (Transform tr in transform)
            {
                if (tr.TryGetComponent<NeutronView>(out NeutronView _))
                    Debug.LogError("Child objects cannot have a Neutron View if their root or parent already has one.");
            }
        }
    }

    private void GetAttributes()
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
                        Dynamic NeutronDynamicAttr = mInfos[y].GetCustomAttribute<Dynamic>();
                        if (NeutronDynamicAttr != null)
                        {
                            RemoteProceduralCall remoteProceduralCall = new RemoteProceduralCall(mInstance, mInfos[y], NeutronDynamicAttr);
                            int uniqueID = NeutronDynamicAttr.ID ^ mInstance.ID;
                            if (!Dynamics.ContainsKey(uniqueID))
                                Dynamics.Add(uniqueID, remoteProceduralCall);
                            else Debug.LogError($"You cannot have the same classes({mInstance.GetType().Name}) with the same ID.");
                            if (mInfos[y].ReturnType == typeof(bool) && !NeutronConfig.Settings.GlobalSettings.SendOnPostProcessing)
                                NeutronUtils.LoggerError($"Boolean return in Dynamic -> {remoteProceduralCall.method.Name} : [{NeutronDynamicAttr.ID}] is useless when \"SendOnPostProcessing\" is disabled, switch to void instead of bool");
                        }
                        else continue;
                    }
                }
                else continue;
            }
        }
        else NeutronUtils.Logger("Could not find any implementation of \"NeutronBehaviour\"");
    }
}