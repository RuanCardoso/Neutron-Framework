// using NeutronNetwork.Naughty.Attributes;
// using NeutronNetwork.Attributes;
// using NeutronNetwork.Constants;
// using NeutronNetwork.Helpers;
// using System;
// using System.Collections;
// using UnityEngine;

// namespace NeutronNetwork.Components
// {
//     /// <summary>
//     ///* Componente usado para sincronizar a física via rede.
//     /// </summary>
//     [RequireComponent(typeof(Rigidbody))]
//     [AddComponentMenu("Neutron/Neutron Rigidbody")]
//     public class NeutronRigidbody : NeutronBehaviour
//     {
//         [Header("[Synchronize Settings]")]
//         [SerializeField] private bool m_SyncVelocity = true;
//         [SerializeField] private bool m_SyncPosition = true;
//         [SerializeField] private bool m_SyncRotation = true;
//         [SerializeField] private bool m_SyncAngularVelocity = true;

//         [Header("[Move Towards]")]
//         [SerializeField] [ShowIf("m_SmoothMode", Smooth.MoveTowards)] private float m_MaxDistanceDelta = 1f;
//         [SerializeField] [ShowIf("m_SmoothMode", Smooth.MoveTowards)] private float m_MaxDegreesDelta = 1f;

//         [Header("[Lerp]")]
//         [SerializeField] [ShowIf("m_SmoothMode", Smooth.Lerp)] private float m_LerpDuration = 1f;

//         [Header("[Smooth Damp]")]
//         [SerializeField] [ShowIf("m_SmoothMode", Smooth.SmoothDamp)] private float m_SmoothTime = 1;
//         [SerializeField] [ShowIf("m_SmoothMode", Smooth.SmoothDamp)] private float m_MaxDampDegreesDelta = 1f;

//         [Header("[Smooth Settings]")]
//         [SerializeField] private float m_TransformUpdateInterval = 0.01f;
//         [SerializeField] Smooth m_SmoothMode = Smooth.MoveTowards;

//         [Header("[Lag Settings]")]
//         [SerializeField] [InfoBox("Lag Settings is experimental.", EInfoBoxType.Warning)] private bool m_LagCompensation = false;
//         [SerializeField] [ShowIf("m_LagCompensation")] private float m_LagMultiplier = 2f;

//         [Header("[Cheater Settings]")]
//         [SerializeField] [InfoBox("Cheat Settings is experimental.", EInfoBoxType.Warning)] private bool m_AntiTeleport = true;
//         [SerializeField] [ShowIf("m_AntiTeleport")] private float m_TeleportDistance = 12f;
//         [SerializeField] [ReadOnly] private bool m_AntiSpeedHack = true;

//         #region States
//         private Vector3 m_Position = Vector3.zero, m_PositionDelta = Vector3.zero;
//         private Quaternion m_Rotation = Quaternion.identity, m_RotationDelta = Quaternion.identity;
//         #endregion

//         #region Timers
//         private float t_TransformUpdateInterval;
//         private float t_LerpDuration;
//         #endregion

//         #region Components
//         private Rigidbody m_Rigidbody;
//         #endregion

//         #region Others
//         private bool m_IsReceived;
//         private Vector3 currentVelocity;
//         #endregion

//         public override void Awake()
//         {
//             base.Awake();
//             {
//                 m_Rigidbody = GetComponent<Rigidbody>();
//             }
//         }

//         protected override void OnNeutronUpdate()
//         {
//             base.OnNeutronUpdate();
//             {
//                 if (!HasAuthority && m_IsReceived)
//                 {
//                     t_TransformUpdateInterval += Time.deltaTime;

//                     #region Delta
//                     if (m_SmoothMode == Smooth.MoveTowards)
//                     {
//                         m_PositionDelta = Vector3.MoveTowards(m_Rigidbody.position, m_Position, m_MaxDistanceDelta * Time.deltaTime);
//                         m_RotationDelta = Quaternion.RotateTowards(m_Rigidbody.rotation, m_Rotation, m_MaxDegreesDelta * Time.deltaTime);
//                     }
//                     else if (m_SmoothMode == Smooth.Lerp)
//                     {
//                         t_LerpDuration += Time.deltaTime;
//                         if (t_LerpDuration < m_LerpDuration)
//                         {
//                             m_PositionDelta = Vector3.Lerp(m_Rigidbody.position, m_Position, t_LerpDuration / m_LerpDuration);
//                             m_RotationDelta = Quaternion.Lerp(m_Rigidbody.rotation, m_Rotation, t_LerpDuration / m_LerpDuration);
//                         }
//                         else
//                         {
//                             m_PositionDelta = m_Position;
//                             m_RotationDelta = m_Rotation;
//                             #region Reset
//                             t_LerpDuration = 0;
//                             #endregion
//                         }
//                     }
//                     else if (m_SmoothMode == Smooth.SmoothDamp)
//                     {
//                         m_PositionDelta = Vector3.SmoothDamp(m_Rigidbody.position, m_Position, ref currentVelocity, m_SmoothTime);
//                         m_RotationDelta = Quaternion.RotateTowards(m_Rigidbody.rotation, m_Rotation, m_MaxDampDegreesDelta * Time.deltaTime);
//                     }
//                     #endregion

//                     #region Physics
//                     if (t_TransformUpdateInterval >= m_TransformUpdateInterval)
//                     {
//                         #region Reset
//                         t_TransformUpdateInterval = 0;
//                         #endregion
//                     }
//                     #endregion
//                 }
//             }
//         }

//         protected override void OnNeutronFixedUpdate()
//         {
//             if (!HasAuthority && m_IsReceived)
//             {
//                 if (m_SyncVelocity) m_Rigidbody.velocity = m_Velocity;
//                 if (m_SyncAngularVelocity) m_Rigidbody.angularVelocity = m_AngularVelocity;
//                 if (m_SyncPosition) m_Rigidbody.position = m_Position;
//                 if (m_SyncRotation) m_Rigidbody.rotation = m_Rotation;
//                 //if (m_SyncPosition) m_Rigidbody.position = m_Position;
//                 //if (m_SyncRotation) m_Rigidbody.rotation = m_Rotation;
//             }
//             else
//             {
//                 testPos = m_Rigidbody.position;
//             }
//         }

//         Vector3 testPos;

//         Vector3 m_Velocity, m_AngularVelocity;
//         public override bool OnSerializeNeutronView(NeutronWriter nWriter, NeutronReader nReader, bool isWriting)
//         {
//             if (m_SyncPosition)
//             {
//                 if (isWriting)
//                     nWriter.Write(testPos);
//                 else if (DoNotPerformTheOperationOnTheServer)
//                     m_Position = nReader.ReadVector3();
//             }
//             if (m_SyncRotation)
//             {
//                 if (isWriting)
//                     nWriter.Write(m_Rigidbody.rotation);
//                 else if (DoNotPerformTheOperationOnTheServer)
//                     m_Rotation = nReader.ReadQuaternion();
//             }
//             if (m_SyncVelocity)
//             {
//                 if (isWriting)
//                     nWriter.Write(m_Rigidbody.velocity);
//                 else if (DoNotPerformTheOperationOnTheServer)
//                     m_Velocity = nReader.ReadVector3();
//             }
//             if (m_SyncAngularVelocity)
//             {
//                 if (isWriting)
//                     nWriter.Write(m_Rigidbody.angularVelocity);
//                 else if (DoNotPerformTheOperationOnTheServer)
//                     m_AngularVelocity = nReader.ReadVector3();
//             }
//             return OnValidateSerialization(isWriting);
//         }

//         //* Valida alguma propriedade, se o retorno for falso, os dados não são enviados.
//         protected override bool OnValidateSerialization(bool IsWriting)
//         {
//             if (IsWriting)
//             {
//                 bool l_IsAnySynced = m_SyncVelocity || m_SyncPosition || m_SyncRotation || m_SyncAngularVelocity;
//                 bool l_IsChanged = (m_Rigidbody.velocity.sqrMagnitude != 0 && m_SyncVelocity) || (m_Rigidbody.angularVelocity.sqrMagnitude != 0 && m_SyncAngularVelocity);
//                 return true;
//             }
//             else
//             {
//                 if (DoNotPerformTheOperationOnTheServer)
//                 {
//                     m_IsReceived = true;
//                 }
//                 return true;
//             }
//         }

//         private void OnValidate()
//         {
//             if (!m_ShowDefaultOptions) m_ShowDefaultOptions = true;
//         }
//     }
// }