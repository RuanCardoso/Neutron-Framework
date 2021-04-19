using UnityEngine;

namespace NeutronNetwork
{
    [AddComponentMenu("Neutron/Neutron View")]
    [DefaultExecutionOrder(NeutronExecutionOrder.NEUTRON_VIEW)]
    public class NeutronView : ViewConfig
    {
        private new void Awake()
        {
            base.Awake(); //* do not remove this line. place your code below it.
        }

        private new void Start()
        {
            base.Start(); //* do not remove this line. place your code below it.
        }

        public override void OnNeutronStart()
        {
            base.OnNeutronStart(); //* do not remove this line. place your code below it.
        }

        public override void OnNeutronAwake()
        {
            base.OnNeutronAwake(); //* do not remove this line. place your code below it.
        }

        private new void Update()
        {
            base.Update(); //* do not remove this line. place your code below it.
        }
    }
}