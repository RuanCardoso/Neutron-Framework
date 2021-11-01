using UnityEngine;

namespace NeutronNetwork.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public class MoveCube : NeutronBehaviour
    {
        private Rigidbody _rb;
        private float _x, _y;

#pragma warning disable IDE0051
        protected override void Awake()
#pragma warning restore IDE0051
        {
            base.Awake();
            {
                _rb = GetComponent<Rigidbody>();
            }
        }

#pragma warning disable IDE0051
        protected override void OnNeutronUpdate()
        {
            base.OnNeutronUpdate();
            {
                if (HasAuthority)
                {
                    _x = Input.GetAxis("Horizontal");
                    _y = Input.GetAxis("Vertical");
                }
            }
        }
#pragma warning restore IDE0051

#pragma warning disable IDE0051
        protected override void OnNeutronFixedUpdate()
#pragma warning restore IDE0051
        {
            base.OnNeutronFixedUpdate();
            {
                if (HasAuthority)
                {
                    Vector3 force = new Vector3(_x, 0, _y) * 35f;
                    _rb.AddForce(force, ForceMode.Force);
                }
            }
        }
    }
}