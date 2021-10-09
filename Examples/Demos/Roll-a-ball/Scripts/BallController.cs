using UnityEngine;

namespace NeutronNetwork.Examples
{
    [RequireComponent(typeof(Rigidbody))]
    public class BallController : MonoBehaviour
    {
        [SerializeField] private float _speed = 5f;
        private Rigidbody _rigidbody;
        private float _movX, _movY;

        void Start()
        {
            _rigidbody = GetComponent<Rigidbody>();
        }

        // Update is called once per frame
        void Update()
        {
            _movX = Input.GetAxis("Horizontal");
            _movY = Input.GetAxis("Vertical");
        }

        private void FixedUpdate()
        {
            Vector3 movement = new Vector3(_movX, 0.0f, _movY);
            _rigidbody.AddForce(movement * _speed, ForceMode.Force);
        }
    }
}