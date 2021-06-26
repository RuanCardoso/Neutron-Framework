using NeutronNetwork.Constants;
using NeutronNetwork.Internal;
using UnityEngine;

namespace NeutronNetwork
{
    /// <summary>
    /// <para>This is your object on the network.</para>
    /// <para>If you want, for example, to obtain a component of the object on the server.</para>
    /// You can declare the function or field here and access it via, for example, "MyPlayer.NeutronView.MyMethod"
    /// </summary>
    [AddComponentMenu("Neutron/Neutron View")]
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_VIEW)]
    public class NeutronView : NeutronViewBehaviour
    {
        #region MonoBehaviour
        private new void Awake()
        {
            base.Awake(); //* do not remove this line. place your code below it.
        }

        private void Start()
        {

        }

        private new void Update()
        {
            base.Update(); //* do not remove this line. place your code below it.
        }
        #endregion

        #region Overrides
        public override void OnNeutronStart()
        {
            base.OnNeutronStart(); //* do not remove this line. place your code below it.
        }

        public override void OnNeutronAwake()
        {
            base.OnNeutronAwake(); //* do not remove this line. place your code below it.
        }
        #endregion
    }
}