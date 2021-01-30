using NeutronNetwork;
using NeutronNetwork.Internal.Client;
using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class ClientView : MonoBehaviour
    {
        [Header("Object")]
        public NeutronProperty neutronProperty;
        public NeutronSyncBehaviour neutronSyncBehaviour;

        [Header("Components")]
        public Rigidbody _rigidbody;
        public CharacterController _controller;

        [NonSerialized] public bool isMine = false;
        [NonSerialized] public Neutron _;

        private void Awake()
        {
            neutronProperty = new NeutronProperty();
            neutronSyncBehaviour = GetComponent<NeutronSyncBehaviour>();
        }

        private void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            _controller = GetComponent<CharacterController>();
        }
    }
}
namespace NeutronNetwork.Internal.Client
{
    [Serializable]
    public class NeutronProperty : IEquatable<NeutronProperty>
    {
        public int ownerID = -1;

        public Boolean Equals(NeutronProperty other)
        {
            return this.ownerID == other.ownerID;
        }
    }
}