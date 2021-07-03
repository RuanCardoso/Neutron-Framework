using NeutronNetwork.Naughty.Attributes;
using NeutronNetwork.Attributes;
using NeutronNetwork.Constants;
using NeutronNetwork.Helpers;
using System;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Components
{
    /// <summary>
    ///* Componente usado para sincronizar a física via rede.
    /// </summary>
    [RequireComponent(typeof(Rigidbody))]
    [AddComponentMenu("Neutron/Neutron Rigidbody")]
    public class NeutronRigidbody : NeutronBehaviour
    {
        [Header("[Synchronize Settings]")]
        [SerializeField] private bool m_SyncVelocity = true;
        [SerializeField] private bool m_SyncPosition = true;
        [SerializeField] private bool m_SyncRotation = true;
        [SerializeField] private bool m_SyncAngularVelocity = true;

        [Header("[Move Towards]")]
        [SerializeField] private float m_MaxDistanceDelta = 2f;
        [SerializeField] private float m_MaxDegreesDelta = 2f;

        [Header("[Lerp]")]
        [SerializeField] [ShowIf("")] private float m_LerpDuration = 1f;

        [Header("[Smooth Damp]")]
        [SerializeField] private float m_SmoothTime = 1f;
        [SerializeField] private float m_MaxDampDegreesDelta = 2f;

        [Header("[Smooth Settings]")]
        [SerializeField] private float m_TransformUpdateInterval = 0.01f;

        [Header("[Lag Compensation Settings]")]
        [SerializeField] private bool m_LagCompensation = true;
        [SerializeField] private float m_LagMultiplier = 3f;

        [Header("[Cheater Settings]")]
        [SerializeField] private bool m_AntiTeleport = true;
        [SerializeField] private float m_TeleportIfDisGreaterThan = 12f;
        [SerializeField] private float m_CheaterIfDisGreaterThan = 15f;
        [SerializeField] private bool m_AntiSpeedHack = true;

        [Header("[General Settings]")]
        [SerializeField] SmoothMode m_SmoothMode = SmoothMode.MoveTowards;

        [Header("[Infor]")]
        [SerializeField] [ReadOnly] private int m_CurrentPacketsPerSecond;
        [SerializeField] [ReadOnly] private int m_MaxPacketsPerSecond;

        private bool m_IsReceived = false;

        #region States
        private Vector3 m_Position, m_PositionDelta;
        private Vector3 m_Velocity, m_AngularVelocity;
        private Quaternion m_Rotation, m_RotationDelta;
        #endregion

        #region Timers
        private float t_TransformUpdateInterval;
        private float t_LerpDuration;
        #endregion

        #region Components
        private Rigidbody m_Rigidbody;
        #endregion

        #region Others
        private Vector3 currentVelocity;
        #endregion

        private new void Awake()
        {
            base.Awake();
            {
                m_Rigidbody = GetComponent<Rigidbody>();
            }
        }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            if ((m_SyncVelocity || m_SyncPosition || m_SyncRotation || m_SyncAngularVelocity) && HasAuthority)
                StartCoroutine(Synchronize());
            else if (IsServer) m_MaxPacketsPerSecond = NeutronHelper.GetMaxPacketsPerSecond(m_SendRate);
        }

        private new void Start()
        {
            base.Start();
            // if (IsServer && !HasAuthority && m_AntiSpeedHack)
            //     StartCoroutine(PacketSpeed());
        }

        private IEnumerator Synchronize()
        {
            while (true)
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    #region Writer
                    if (m_SyncPosition) nWriter.Write(m_Rigidbody.position);
                    if (m_SyncVelocity) nWriter.Write(m_Rigidbody.velocity);
                    if (m_SyncRotation) nWriter.Write(m_Rigidbody.rotation);
                    if (m_SyncAngularVelocity) nWriter.Write(m_Rigidbody.angularVelocity);
                    nWriter.Write(NeutronView._.CurrentTime);
                    #endregion

                    #region Send
                    if ((m_Rigidbody.velocity.sqrMagnitude != 0 && m_SyncVelocity) || (m_Rigidbody.angularVelocity.sqrMagnitude != 0 && m_SyncAngularVelocity))
                        iRPC(NeutronConstants.NEUTRON_RIGIDBODY, nWriter, CacheMode.Overwrite, m_SendTo, m_BroadcastTo, m_ReceivingProtocol, m_SendingProtocol);
                    #endregion
                }
                yield return new WaitForSeconds(NeutronConstants.ONE_PER_SECOND / m_SendRate);
            }
        }

        [iRPC(NeutronConstants.NEUTRON_RIGIDBODY, true)]
        private void RPC(NeutronReader nReader, bool nIsMine, Player nSender)
        {
            #region Reader
            if (m_SyncPosition) m_Position = nReader.ReadVector3();
            if (m_SyncVelocity) m_Velocity = nReader.ReadVector3();
            if (m_SyncRotation) m_Rotation = nReader.ReadQuaternion();
            if (m_SyncAngularVelocity) m_AngularVelocity = nReader.ReadVector3();
            double l_Time = nReader.ReadDouble();
            #endregion

            //* Define se o primeiro pacote de atualização chegou.
            m_IsReceived = true;

            #region Lag Compensation
            if (m_LagCompensation)
            {
                float l_CurrentTime = 0;
                if (!IsServer)
                    l_CurrentTime = Math.Abs((float)(NeutronView._.CurrentTime - l_Time));
                else l_CurrentTime = Math.Abs((float)(Neutron.Server.CurrentTime - l_Time));
                m_Position += (m_Velocity * l_CurrentTime) * m_LagMultiplier;
            }
            #endregion



            //             m_IsOn = true;
            //             using (options)
            //             {
            // #if UNITY_SERVER || UNITY_EDITOR
            //                 if (IsServer)
            //                     ++m_CurrentPacketsPerSecond;
            // #endif
            //                 if (m_SyncPosition) m_Position = options.ReadVector3();
            //                 if (m_SyncVelocity) m_Rigidbody.velocity = options.ReadVector3();
            //                 if (m_SyncRotation) m_Rotation = options.ReadQuaternion();
            //                 if (m_SyncAngularVelocity) m_Rigidbody.angularVelocity = options.ReadVector3();

            //                 if (IsServer && m_LagCompensation)
            //                 {
            //                     // float lag = (float)Math.Abs(Neutron.Server.CurrentTime - infor.SentClientTime) + synchronizeInterval;
            //                     // position += neutronRigidbody.velocity * (lag / lagMultiplier);
            //                 }
            //                 if (m_SyncPosition) TeleportByDistance();
            //             }
        }

        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            {
                t_TransformUpdateInterval += Time.deltaTime;
                if (!HasAuthority && m_IsReceived)
                {
                    #region Delta
                    if (m_SmoothMode == SmoothMode.MoveTowards)
                    {
                        m_PositionDelta = Vector3.MoveTowards(m_Rigidbody.position, m_Position, m_MaxDistanceDelta * Time.deltaTime);
                        m_RotationDelta = Quaternion.RotateTowards(m_Rigidbody.rotation, m_Rotation, m_MaxDegreesDelta * Time.deltaTime);
                    }
                    else if (m_SmoothMode == SmoothMode.Lerp)
                    {
                        t_LerpDuration += Time.deltaTime;
                        if (t_LerpDuration < m_LerpDuration)
                        {
                            m_PositionDelta = Vector3.Lerp(m_Rigidbody.position, m_Position, t_LerpDuration / m_LerpDuration);
                            m_RotationDelta = Quaternion.Lerp(m_Rigidbody.rotation, m_Rotation, t_LerpDuration / m_LerpDuration);
                        }
                        else
                        {
                            m_PositionDelta = m_Position;
                            m_RotationDelta = m_Rotation;
                            #region Reset
                            t_LerpDuration = 0;
                            #endregion
                        }
                    }
                    else if (m_SmoothMode == SmoothMode.SmoothDamp)
                    {
                        m_PositionDelta = Vector3.SmoothDamp(m_Rigidbody.position, m_Position, ref currentVelocity, m_SmoothTime);
                        m_RotationDelta = Quaternion.RotateTowards(m_Rigidbody.rotation, m_Rotation, m_MaxDampDegreesDelta * Time.deltaTime);
                    }
                    #endregion

                    #region Physics
                    if (t_TransformUpdateInterval >= m_TransformUpdateInterval)
                    {
                        if (m_SyncPosition) m_Rigidbody.position = m_PositionDelta;
                        if (m_SyncRotation) m_Rigidbody.rotation = m_RotationDelta;
                        #region Reset
                        t_TransformUpdateInterval = 0;
                        #endregion
                    }
                    #endregion
                }
            }
        }

        protected override void OnNeutronFixedUpdate()
        {
            base.OnNeutronFixedUpdate();
            {
                if (!HasAuthority && m_IsReceived)
                {
                    if (m_SyncVelocity) m_Rigidbody.velocity = m_Velocity;
                    if (m_SyncAngularVelocity) m_Rigidbody.angularVelocity = m_AngularVelocity;
                }
            }
        }

        //         private void TeleportByDistance()
        //         {
        //             Vector3 lagDistance = m_Position - transform.position;
        //             if (lagDistance.magnitude > m_TeleportIfDisGreaterThan)
        //             {
        // #if UNITY_SERVER || UNITY_EDITOR
        //                 if (IsServer && m_AntiTeleport)
        //                     CheatsHelper.Teleport(lagDistance, (m_TeleportIfDisGreaterThan + m_CheaterIfDisGreaterThan), NeutronView.Owner);
        // #endif
        //                 transform.position = m_Position;
        //             }
        //         }

        //         private IEnumerator PacketSpeed()
        //         {
        //             while (IsServer)
        //             {
        //                 CheatsHelper.SpeedHack(m_CurrentPacketsPerSecond, m_MaxPacketsPerSecond, NeutronView.Owner);
        //                 m_CurrentPacketsPerSecond = 0;
        //                 yield return new WaitForSeconds(1f);
        //             }
        //         }

        //         private void SmoothMovement()
        //         {
        //             if (m_SmoothMode == SmoothMode.MoveTowards)
        //             {
        //                 if (m_SyncPosition) m_Rigidbody.position = Vector3.MoveTowards(m_Rigidbody.position, m_Position, m_Smooth * Time.fixedDeltaTime);
        //                 if (m_SyncRotation) m_Rigidbody.rotation = Quaternion.RotateTowards(m_Rigidbody.rotation, m_Rotation, m_Smooth * Time.fixedDeltaTime);
        //             }
        //             else if (m_SmoothMode == SmoothMode.Lerp)
        //             {
        //                 if (m_SyncPosition) m_Rigidbody.position = Vector3.Lerp(m_Rigidbody.position, m_Position, m_Smooth * Time.fixedDeltaTime);
        //                 if (m_SyncRotation) m_Rigidbody.rotation = Quaternion.Slerp(m_Rigidbody.rotation, m_Rotation, m_Smooth * Time.fixedDeltaTime);
        //             }
        //         }

        //         protected override void OnNeutronFixedUpdate()
        //         {
        //             base.OnNeutronFixedUpdate();
        //             if (!HasAuthority && !m_IsOn) return;
        //             if (IsClient)
        //             {
        //                 if (!HasAuthority)
        //                 {
        //                     SmoothMovement();
        //                 }
        //             }
        //             else if (IsServer)
        //             {
        //                 if (!HasAuthority)
        //                 {
        //                     if (m_SmoothOnServer)
        //                         SmoothMovement();
        //                     else
        //                     {
        //                         if (m_SyncPosition) m_Rigidbody.position = m_Position;
        //                         if (m_SyncRotation) m_Rigidbody.rotation = m_Rotation;
        //                     }
        //                 }
        //             }
        //         }
    }
}