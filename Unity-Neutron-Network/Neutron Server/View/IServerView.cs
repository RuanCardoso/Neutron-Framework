using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.AI;

public class ServerView : Properties
{
    [NonSerialized] public NeutronSyncBehaviour neutronSyncBehaviour;
    //-------------------------------------------------
    public Player player;

    private void Awake()
    {
        neutronSyncBehaviour = GetComponent<NeutronSyncBehaviour>();
        neutronSyncBehaviour.Init();
    }

    private void Start()
    {
        _rigidbody = GetComponent<Rigidbody>();
        _controller = GetComponent<CharacterController>();
    }

    private void Update()
    {
        lastPosition = transform.position;
        lastRotation = transform.eulerAngles;
    }
}

public class Properties : PlayerComponents
{
    [Header("Properties")]
    public Vector3 lastPosition;
    public Vector3 lastRotation;
}

public class PlayerComponents : MonoBehaviour
{
    [Header("Components")]
    public Rigidbody _rigidbody;
    public CharacterController _controller;
}