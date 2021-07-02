using NeutronNetwork.Attributes;
using NeutronNetwork.Client.Internal;
using NeutronNetwork.Constants;
using System.Collections;
using UnityEngine;

namespace NeutronNetwork.Components
{
    /// <summary>
    ///* Este componente irá sincronizar os estados das variáveis do animator.
    /// </summary>
    [RequireComponent(typeof(Animator))]
    [AddComponentMenu("Neutron/Neutron Animator")]
    public class NeutronAnimator : NeutronBehaviour
    {
        [Header("[Component]")]
        /// <summary>
        ///* O Componente animator anexado ao objeto.
        /// </summary>
        [ReadOnly] public Animator m_Animator;

        [Header("[Parameters Settings]")]
        /// <summary>
        ///* Os parâmetros do animator que serão sincronizados via rede.
        /// </summary>
        public NeutronAnimatorParameter[] m_Parameters;

        [Header("[General Settings]")]
        [SerializeField] [Range(0, 1)] private float m_SynchronizeInterval = 0.1f;
        [SerializeField] private CacheMode m_CacheMode = CacheMode.Overwrite;
        [SerializeField] private SendTo m_SendTo = SendTo.Others;
        [SerializeField] private Broadcast m_Broadcast = Broadcast.Room;
        [SerializeField] private Protocol m_Protocol = Protocol.Udp;

        public override void OnNeutronStart()
        {
            base.OnNeutronStart();
            if (HasAuthority)
                StartCoroutine(Synchronize()); //* Inicia a sincronização.
        }

        private IEnumerator Synchronize()
        {
            while (true)
            {
                using (NeutronWriter nWriter = Neutron.PooledNetworkWriters.Pull())
                {
                    nWriter.SetLength(0);
                    for (int i = 0; i < m_Parameters.Length; i++)
                    {
                        var cParam = m_Parameters[i];
                        if (cParam.parameterMode == ParameterMode.NonSync) continue;
                        else
                        {
                            //* Percorre os parâmetros e escreve os seus valores na rede.
                            switch (cParam.parameterType)
                            {
                                case AnimatorControllerParameterType.Float:
                                    nWriter.Write(m_Animator.GetFloat(cParam.parameterName));
                                    break;
                                case AnimatorControllerParameterType.Int:
                                    nWriter.Write(m_Animator.GetInteger(cParam.parameterName));
                                    break;
                                case AnimatorControllerParameterType.Bool:
                                    nWriter.Write(m_Animator.GetBool(cParam.parameterName));
                                    break;
                                case AnimatorControllerParameterType.Trigger:
                                    break;
                            }
                        }
                    }
                    iRPC(NeutronConstants.NEUTRON_ANIMATOR, nWriter, m_CacheMode, m_SendTo, m_Broadcast, m_Protocol); //* envia pra rede.
                }
                yield return new WaitForSeconds(m_SynchronizeInterval);
            }
        }

        [iRPC(NeutronConstants.NEUTRON_ANIMATOR, true)]
        private void RPC(NeutronReader nReader, bool nIsMine, Player nSender)
        {
            using (nReader)
            {
                for (int i = 0; i < m_Parameters.Length; i++)
                {
                    var cParam = m_Parameters[i];
                    if (cParam.parameterMode == ParameterMode.NonSync) continue;
                    else
                    {
                        //* Percorre os parâmetros e ler os seus valores da rede.
                        switch (cParam.parameterType)
                        {
                            case AnimatorControllerParameterType.Float:
                                m_Animator.SetFloat(cParam.parameterName, nReader.ReadSingle());
                                break;
                            case AnimatorControllerParameterType.Int:
                                m_Animator.SetInteger(cParam.parameterName, nReader.ReadInt32());
                                break;
                            case AnimatorControllerParameterType.Bool:
                                m_Animator.SetBool(cParam.parameterName, nReader.ReadBoolean());
                                break;
                            case AnimatorControllerParameterType.Trigger:
                                break;
                        }
                    }
                }
            }
        }
    }
}