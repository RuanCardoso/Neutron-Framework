using System;
using System.Collections;
using System.Collections.Generic;
using NeutronNetwork;
using UnityEngine;

public class IViewConfig : MonoBehaviour
{
    public Player owner;
    [NonSerialized] public Neutron _;
    [NonSerialized] public bool isServerOrClient;
    [NonSerialized] public NeutronSyncBehaviour neutronSyncBehaviour;

    public Vector3 lastPosition { get; set; }
    public Vector3 lastRotation { get; set; }

    public NeutronBehaviour[] neutronBehaviours { get; private set; }

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
        Utilities.LoggerWarning("Component Destroyed");
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