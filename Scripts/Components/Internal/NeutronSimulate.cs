using UnityEngine;

namespace NeutronNetwork.Server.Internal
{
    public class NeutronSimulate : MonoBehaviour
    {
        public PhysicsScene PhysicsScene { get; set; }
        private float _timer;

        private void Update()
        {
            if (!PhysicsScene.IsValid())
                return; // do nothing if the physics Scene is not valid.

            _timer += Time.deltaTime;

            // Catch up with the game time.
            // Advance the physics simulation in portions of Time.fixedDeltaTime
            // Note that generally, we don't want to pass variable delta to Simulate as that leads to unstable results.
            while (_timer >= Time.fixedDeltaTime)
            {
                _timer -= Time.fixedDeltaTime;
                PhysicsScene.Simulate(Time.fixedDeltaTime);
            }

            // Here you can access the transforms state right after the simulation, if needed...
        }
    }
}