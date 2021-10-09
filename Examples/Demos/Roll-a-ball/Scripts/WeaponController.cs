using UnityEngine;

namespace NeutronNetwork.Examples
{
    public class WeaponController : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private GameObject _bullet;
        [SerializeField] private Transform _bulletSpawn;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private float _rotationSpeed = 5f;

        private void Start()
        {
            _offset += transform.localPosition - _player.transform.localPosition;
        }


        private void Update()
        {
            if (Input.GetMouseButtonDown(0))
                Instantiate(_bullet, _bulletSpawn.position, _bulletSpawn.rotation);
        }

        void LateUpdate()
        {
            transform.localPosition = _player.transform.localPosition + _offset;
            transform.Rotate(transform.up, (-Input.GetAxis("Mouse X")) * _rotationSpeed * Time.deltaTime);
        }
    }
}