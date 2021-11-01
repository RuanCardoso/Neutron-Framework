using UnityEngine;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Physics Simulator")]
    public class PhysicsSimulator : MonoBehaviour
    {
        public PhysicsScene PhysicsScene { get; private set; }
        void Start()
        {
            PhysicsScene = gameObject.scene.GetPhysicsScene();
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            PhysicsScene.Simulate(Time.fixedDeltaTime);
        }
    }
}