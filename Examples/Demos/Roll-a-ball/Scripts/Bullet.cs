using UnityEngine;

namespace NeutronNetwork.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public class Bullet : MonoBehaviour
    {
        private Rigidbody _rigidbody;
        private bool _isShoted = false;
        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
            Destroy(gameObject, 3f);
        }

        private void FixedUpdate()
        {
            if (!_isShoted)
            {
                _rigidbody.AddForce(-transform.forward * 1800, ForceMode.Force);
                _isShoted = true;
            }
        }
    }
}