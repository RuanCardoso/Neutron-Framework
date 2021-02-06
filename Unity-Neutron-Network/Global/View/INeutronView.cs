using System;
using System.Net.Sockets;
using UnityEngine;
using UnityEngine.AI;

namespace NeutronNetwork
{
    public class NeutronView : MonoBehaviour
    {
        [NonSerialized] public NeutronSyncBehaviour neutronSyncBehaviour;
        [NonSerialized] public Neutron _;
        [NonSerialized] public Player owner;
        [NonSerialized] public bool isServerOrClient;

        public Vector3 lastPosition;
        public Vector3 lastRotation;

        private void Awake()
        {
            neutronSyncBehaviour = GetComponent<NeutronSyncBehaviour>();
        }

        private void Start()
        {
            
        }

        private void Update()
        {
            lastPosition = transform.position;
            lastRotation = transform.eulerAngles;
        }
    }
}