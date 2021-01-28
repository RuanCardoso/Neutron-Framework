using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[AddComponentMenu("Neutron/Simulate SpeedHack")]
public class SimulateSpeedHack : MonoBehaviour {
    [Range (0, 20)]
    [SerializeField] private float _timeScale = 1;
    void FixedUpdate () {
        if (Time.timeScale != _timeScale) Time.timeScale = _timeScale;
    }
}