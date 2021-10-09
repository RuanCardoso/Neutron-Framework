using UnityEngine;

namespace NeutronNetwork.Examples
{
    public class CameraController : MonoBehaviour
    {
        [SerializeField] private GameObject _player;
        [SerializeField] private Vector3 _offset;
        [SerializeField] private float _rotationSpeed = 5f;
        private float _movX;

        // Start is called before the first frame update
        void Start()
        {
            _offset += transform.localPosition - _player.transform.localPosition;
            MouseLock();
        }

        void Update()
        {
            _movX = Input.GetAxis("Mouse X") * _rotationSpeed * Time.deltaTime;
            if (Input.GetKeyDown(KeyCode.Escape))
                MouseLock();
        }

        // Update is called once per frame
        void LateUpdate()
        {
            transform.localPosition = _player.transform.localPosition + _offset;
            transform.localRotation *= Quaternion.AngleAxis(_movX, Vector3.forward);
        }

        private void MouseLock()
        {
            Cursor.visible = !Cursor.visible;
            Cursor.lockState = !Cursor.visible ? CursorLockMode.Locked : CursorLockMode.None;
        }
    }
}