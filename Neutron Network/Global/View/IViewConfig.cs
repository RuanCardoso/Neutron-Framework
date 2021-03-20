using System;
using NeutronNetwork;
using NeutronNetwork.Internal.Attributes;
using UnityEngine;

public class ViewConfig : MonoBehaviour
{
    public int ID;
    public AuthorityMode authorityMode;
    [ReadOnly] public Player owner;
    [NonSerialized] public bool isServer;
    [NonSerialized] public Neutron _;
    [NonSerialized] public NeutronSyncBehaviour neutronSyncBehaviour;

    public Vector3 lastPosition { get; set; }
    public Vector3 lastRotation { get; set; }

    public NeutronBehaviour[] neutronBehaviours { get; private set; }
    public bool IsSceneObject { get => (ID > 0 && ID < Neutron.generateID); }

    public void Awake()
    {
        GetSyncBehaviour();
        GetNeutronBehaviours();
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

    public void ResetBehaviours()
    {
        NeutronUtils.LoggerWarning("Component Destroyed");
        GetNeutronBehaviours();
    }

    private void GetNeutronBehaviours()
    {
        neutronBehaviours = GetComponentsInChildren<NeutronBehaviour>();
    }

    private void GetSyncBehaviour()
    {
        neutronSyncBehaviour = GetComponent<NeutronSyncBehaviour>();
    }
}