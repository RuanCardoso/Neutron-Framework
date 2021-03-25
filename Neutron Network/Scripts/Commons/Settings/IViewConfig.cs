using System;
using System.Collections.Generic;
using System.Reflection;
using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

public class ViewConfig : MonoBehaviour
{
    #region Primitives
    public int ID;
    public bool IsSceneObject { get => (ID > 0 && ID < Neutron.GENERATE_ID); }
    [NonSerialized] public bool isServer;
    #endregion

    #region Register
    public AuthorityMode authorityMode;
    [ReadOnly] public Player owner;
    [NonSerialized] public Neutron _;
    #endregion

    #region FindInstances
    [NonSerialized] public NeutronSyncBehaviour neutronSyncBehaviour;
    #endregion

    #region Collection
    public Dictionary<int, RemoteProceduralCall> Dynamics = new Dictionary<int, RemoteProceduralCall>();
    #endregion

    #region UnityEngine
    public Vector3 lastPosition { get; set; }
    public Vector3 lastRotation { get; set; }
    #endregion

    public void Awake()
    {
        GetSyncBehaviour();
        GetAttributes();
    }

    public void Start()
    {

    }

    public virtual void OnNeutronStart()
    {

    }

    public virtual void OnNeutronAwake()
    {

    }

    public void Update()
    {
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
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
                            RemoteProceduralCall remoteProceduralCall = new RemoteProceduralCall(mInstance, mInfos[y]);
                            Dynamics.Add(NeutronDynamicAttr.ID, remoteProceduralCall);
                        }
                        else continue;
                    }
                }
                else continue;
            }
        }
        else NeutronUtils.Logger("Could not find any implementation of \"NeutronBehaviour\"");
    }

    private void GetSyncBehaviour()
    {
        neutronSyncBehaviour = GetComponent<NeutronSyncBehaviour>();
    }
}