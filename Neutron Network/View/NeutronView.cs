using NeutronNetwork.Constants;
using NeutronNetwork.Internal;
using UnityEngine;

namespace NeutronNetwork
{
    //* Faça oque você quiser aqui, só lembre-se de salvar em algum lugar, pra não perder suas implementações quando att, use o GIT (:
    //* Não poder ser herdado, falha de estrutura, vai ficar assim mermo.
    /// <summary>
    ///* Este é o seu objeto na rede e também é o seu objeto de rede, o seu RG.
    /// </summary>
    [AddComponentMenu("Neutron/Neutron View")]
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_VIEW)]
    public class NeutronView : NeutronViewBehaviour
    {
        #region MonoBehaviour
        private new void Awake()
        {
            base.Awake(); //* não remova esta linha. coloque seu código abaixo dele.
        }

        private void Start()
        {

        }

        private new void Update()
        {
            base.Update(); //* não remova esta linha. coloque seu código abaixo dele.
        }
        #endregion

        #region Overrides
        public override void OnNeutronStart()
        {
            base.OnNeutronStart(); //* não remova esta linha. coloque seu código abaixo dele.
        }

        public override void OnNeutronAwake()
        {
            base.OnNeutronAwake(); //* não remova esta linha. coloque seu código abaixo dele.
        }
        #endregion
    }
}