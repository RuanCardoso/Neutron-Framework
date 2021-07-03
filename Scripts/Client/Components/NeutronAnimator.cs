using NeutronNetwork.Client.Internal;
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
        public NeutronAnimatorParameter[] m_Parameters;

        public override bool OnNeutronSerializeView(NeutronWriter nWriter, NeutronReader nReader, bool isWriting)
        {
            for (int i = 0; i < m_Parameters.Length; i++)
            {
                var cParam = m_Parameters[i];
                if (cParam.parameterMode == ParameterMode.NonSync)
                    continue;
                else
                {
                    //* Percorre os parâmetros, escreve e ler os seus valores.
                    switch (cParam.parameterType)
                    {
                        case AnimatorControllerParameterType.Float:
                            {
                                if (isWriting)
                                    nWriter.Write(m_Animator.GetFloat(cParam.parameterName));
                                else
                                    m_Animator.SetFloat(cParam.parameterName, nReader.ReadSingle());
                            }
                            break;
                        case AnimatorControllerParameterType.Int:
                            {
                                if (isWriting)
                                    nWriter.Write(m_Animator.GetInteger(cParam.parameterName));
                                else
                                    m_Animator.SetInteger(cParam.parameterName, nReader.ReadInt32());
                            }
                            break;
                        case AnimatorControllerParameterType.Bool:
                            {
                                if (isWriting)
                                    nWriter.Write(m_Animator.GetBool(cParam.parameterName));
                                else
                                    m_Animator.SetBool(cParam.parameterName, nReader.ReadBoolean());
                            }
                            break;
                        case AnimatorControllerParameterType.Trigger:
                            break;
                    }
                }
            }
            return OnValidate();
        }

        //* Valida alguma propriedade, se o retorno for falso, os dados não são enviados.
        private bool OnValidate()
        {
            return true;
        }
    }
}