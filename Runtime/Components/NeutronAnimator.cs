using NeutronNetwork.Editor;
using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Naughty.Attributes;
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
        /// <summary>
        ///* O Componente animator anexado ao objeto.
        /// </summary>
        [Header("[Component]")]
        [ReadOnly] [InfoBox("Trigger type parameters are not supported.", EInfoBoxType.Warning)] public Animator m_Animator;

        /// <summary>
        ///* Os parâmetros do animator que serão sincronizados via rede.
        /// </summary>
        [Header("[Parameters Settings]")]
        public AnimatorParameter[] m_Parameters;

        //* Sincroniza as variaveis.
        public override bool OnAutoSynchronization(NeutronStream stream, bool isMine)
        {
            var writer = stream.Writer;
            var reader = stream.Reader;
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var cParam = m_Parameters[i];
                if (cParam.SyncMode == SyncOnOff.NonSync)
                    continue;
                else
                {
                    //* Percorre os parâmetros, escreve e ler os seus valores.
                    switch (cParam.ParameterType)
                    {
                        case AnimatorControllerParameterType.Float:
                            {
                                if (isMine)
                                    writer.Write(m_Animator.GetFloat(cParam.ParameterName));
                                else if (DoNotPerformTheOperationOnTheServer)
                                    m_Animator.SetFloat(cParam.ParameterName, reader.ReadFloat());
                            }
                            break;
                        case AnimatorControllerParameterType.Int:
                            {
                                if (isMine)
                                    writer.Write(m_Animator.GetInteger(cParam.ParameterName));
                                else if (DoNotPerformTheOperationOnTheServer)
                                    m_Animator.SetInteger(cParam.ParameterName, reader.ReadInt());
                            }
                            break;
                        case AnimatorControllerParameterType.Bool:
                            {
                                if (isMine)
                                    writer.Write(m_Animator.GetBool(cParam.ParameterName));
                                else if (DoNotPerformTheOperationOnTheServer)
                                    m_Animator.SetBool(cParam.ParameterName, reader.ReadBool());
                            }
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                    writer.Write();
                }
            }
            return OnValidateAutoSynchronization(isMine);
        }

        //* Valida alguma propriedade, se o retorno for falso, os dados não são enviados.
        protected override bool OnValidateAutoSynchronization(bool isMine) => !isMine || m_Parameters.Length > 0;
    }
}