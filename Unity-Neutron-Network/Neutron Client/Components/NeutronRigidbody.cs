using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Server.Cheats;
using System;
using System.Collections;
using System.Net.Sockets;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    public class NeutronRigidbody : NeutronBehaviour
    {
        [SerializeField] private bool synchronizeVelocity = true;
        [SerializeField] private bool synchronizeAngularVelocity = true;
        [SerializeField] private float synchronizeDelay = 0.1f;
        [SerializeField] private SendTo sendTo = SendTo.Others;
        [SerializeField] private Broadcast broadcast = Broadcast.Room;
        [SerializeField] private Protocol protocol = Protocol.Udp;
        private Vector3 velocity;
        private Vector3 angularVelocity;
        private Rigidbody _rigidbody;

        private void Awake()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        public override void OnNeutronStart()
        {
            if (IsClient && (synchronizeVelocity || synchronizeAngularVelocity) && IsMine)
                StartCoroutine(Synchronize());
        }

        private IEnumerator Synchronize()
        {
            while (true)
            {
                using (NeutronWriter options = new NeutronWriter())
                {
                    if (synchronizeVelocity) options.Write(_rigidbody.velocity);
                    if (synchronizeAngularVelocity) options.Write(_rigidbody.angularVelocity);
                    if (_rigidbody.velocity != Vector3.zero || _rigidbody.angularVelocity != Vector3.zero)
                        NeutronView._.RPC(10012, options, sendTo, false, broadcast, protocol);
                }
                yield return new WaitForSeconds(synchronizeDelay);
            }
        }

        [RPC(10012)]
        private void RPC(NeutronReader options)
        {
            if (synchronizeVelocity) velocity = options.ReadVector3();
            if (synchronizeAngularVelocity) angularVelocity = options.ReadVector3();
        }

        private void FixedUpdate()
        {
            if (!IsMine)
            {
                if (synchronizeVelocity) _rigidbody.velocity = velocity;
                if (synchronizeAngularVelocity) _rigidbody.angularVelocity = angularVelocity;
            }
        }
    }
}