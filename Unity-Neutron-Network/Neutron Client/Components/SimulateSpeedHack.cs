using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Simulate SpeedHack")]
    public class SimulateSpeedHack : MonoBehaviour
    {
        [SerializeField] [Range(0, 20)] private float _timeScale = 1;
        void FixedUpdate()
        {
            if (Time.timeScale != _timeScale) Time.timeScale = _timeScale;
        }
    }
}