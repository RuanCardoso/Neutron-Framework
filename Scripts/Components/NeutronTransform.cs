using System;
using System.Collections;
using NeutronNetwork;
using NeutronNetwork.Attributes;
using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Attributes;
using NeutronNetwork.Naughty.Attributes;
using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Transform")]
    public class NeutronTransform : NeutronBehaviour
    {
        //        [Header("[Synchronize Settings]")]
        //        [SerializeField] private bool synchronizePosition = true;
        //        [SerializeField] private bool synchronizeRotation = true;
        //        [SerializeField] private bool synchronizeScale;

        //        [Header("[Smooth Settings]")]
        //        [SerializeField] [Range(0, 1f)] private float synchronizeInterval = 0.1f;
        //        [SerializeField] [Range(0, 30f)] private float Smooth = 10f;
        //        [SerializeField] private bool smoothOnServer = true;

        //        [Header("[Lag Compensation Settings]")]
        //        [SerializeField] private bool lagCompensation = true;
        //        [SerializeField] private float lagMultiplier = 3f;

        //        [Header("[Cheater Settings]")]
        //        [SerializeField] private bool antiTeleport = true;
        //        [SerializeField] private float teleportIfDistanceGreaterThan = 12f;
        //        [SerializeField] private float isCheaterIfDistanceGreaterThan = 15f;
        //        [SerializeField] private bool antiSpeedHack = true;

        //        [Header("[General Settings]")]
        //        [SerializeField] SmoothType smoothMode;
        //        [SerializeField] private TargetTo sendTo = TargetTo.Others;
        //        [SerializeField] private TunnelingTo broadcast = TunnelingTo.Room;
        //        [SerializeField] private Protocol protocol = Protocol.Udp;

        //        [Header("[Infor]")]
        //        [SerializeField] [ReadOnly] private int currentPacketsPerSecond;
        //        [SerializeField] [ReadOnly] private int maxPacketsPerSecond;
        //        private Vector3 position, scale;
        //        private Quaternion rotation;
        //        private bool onFirstPacket = false;

        //        public override void OnNeutronStart()
        //        {
        //            base.OnNeutronStart();
        //            if ((synchronizePosition || synchronizeRotation || synchronizeScale) && HasAuthority)
        //                StartCoroutine(Synchronize());
        //            else if (IsServer) maxPacketsPerSecond = 100;
        //        }

        ////        private new void Start()
        ////        {
        ////            base.Start();
        ////#if UNITY_SERVER || UNITY_EDITOR
        ////            if (IsServer && !HasAuthority && !IsClient && antiSpeedHack)
        ////                StartCoroutine(PacketSpeed());
        ////#endif
        ////        }

        //        private void ResetTransforms()
        //        {
        //            if (synchronizePosition) position = transform.position;
        //            if (synchronizeRotation) rotation = transform.rotation;
        //            if (synchronizeScale) scale = transform.localScale;
        //        }

        //        private IEnumerator Synchronize()
        //        {
        //            while (true)
        //            {
        //                using (var options = Neutron.PooledNetworkWriters.Pull())
        //                {
        //                    if (synchronizePosition) options.Write(transform.position);
        //                    if (synchronizeRotation) options.Write(transform.rotation);
        //                    if (synchronizeScale) options.Write(transform.localScale);
        //                    if (transform.position != position && synchronizePosition || transform.rotation != rotation && synchronizeRotation || transform.localScale != scale && synchronizeScale)
        //                        //iRPC(10013, options, Cache.Overwrite, sendTo, broadcast, protocol);
        //                    if (sendTo == TargetTo.Others || sendTo == TargetTo.Me)
        //                        ResetTransforms();
        //                }
        //                yield return new WaitForSeconds(synchronizeInterval);
        //            }
        //        }

        //        [iRPC(ID = 101)]
        //        private void RPC(NeutronReader options, NeutronPlayer sender)
        //        {
        //            onFirstPacket = true;
        //            using (options)
        //            {
        //#if UNITY_SERVER || UNITY_EDITOR
        //                if (IsServer)
        //                    ++currentPacketsPerSecond;
        //#endif
        //                Vector3 oldPos = transform.position;
        //                if (synchronizePosition) position = options.ReadVector3();
        //                if (synchronizeRotation) rotation = options.ReadQuaternion();
        //                if (synchronizeScale) scale = options.ReadVector3();
        //                if (IsServer && lagCompensation)
        //                {
        //                    // float lag = (float)Math.Abs(Neutron.Server.CurrentTime - infor.SentClientTime) + synchronizeInterval;
        //                    // position += (transform.position - oldPos) * (lag / lagMultiplier);
        //                }
        //                if (synchronizePosition) TeleportByDistance();
        //            }
        //        }

        //        private void TeleportByDistance()
        //        {
        ////             Vector3 lagDistance = position - transform.position;
        ////             if (lagDistance.magnitude > teleportIfDistanceGreaterThan)
        ////             {
        //// #if UNITY_SERVER || UNITY_EDITOR
        ////                 if (IsServer && antiTeleport)
        ////                     CheatsHelper.Teleport(lagDistance, (teleportIfDistanceGreaterThan + isCheaterIfDistanceGreaterThan), NeutronView.Owner);
        //// #endif
        ////                 transform.position = position;
        ////             }
        //        }

        //        private IEnumerator PacketSpeed()
        //        {
        //            while (IsServer)
        //            {
        //                //CheatsHelper.SpeedHack(currentPacketsPerSecond, maxPacketsPerSecond, NeutronView.Owner);
        //                currentPacketsPerSecond = 0;
        //                yield return new WaitForSeconds(1f);
        //            }
        //        }

        //        private void SmoothMovement()
        //        {
        //            if (smoothMode == SmoothType.Lerp)
        //            {
        //                if (synchronizePosition) transform.position = Vector3.Lerp(transform.position, position, Smooth * Time.deltaTime);
        //                if (synchronizeRotation) transform.rotation = Quaternion.Slerp(transform.rotation, rotation, Smooth * Time.deltaTime);
        //            }
        //            else if (smoothMode == SmoothType.MoveTowards)
        //            {
        //                if (synchronizePosition) transform.position = Vector3.MoveTowards(transform.position, position, Smooth * Time.deltaTime);
        //                if (synchronizeRotation) transform.rotation = Quaternion.RotateTowards(transform.rotation, rotation, Smooth * Time.deltaTime);
        //            }
        //        }

        //        //* Update is called once per frame
        //        protected override void OnNeutronUpdate()
        //        {
        //            base.OnNeutronUpdate();
        //            if (!HasAuthority && !onFirstPacket) return;
        //            if (!HasAuthority && synchronizeScale) transform.localScale = scale;
        //            if (IsClient)
        //            {
        //                if (!HasAuthority)
        //                {
        //                    SmoothMovement();
        //                }
        //            }
        //            else if (IsServer)
        //            {
        //                if (!HasAuthority)
        //                {
        //                    if (smoothOnServer)
        //                        SmoothMovement();
        //                    else
        //                    {
        //                        if (synchronizePosition) transform.position = position;
        //                        if (synchronizeRotation) transform.rotation = rotation;
        //                    }
        //                }
        //            }
        //        }
    }
}