using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Components
{
    [AddComponentMenu("Neutron/Neutron Physics Simulator")]
    public class NeutronPhysicsSimulator : MonoBehaviour
    {
        public LocalPhysicsMode LocalPhysicsMode { get; private set; }
        public PhysicsScene PhysicsScene { get; private set; }
        public PhysicsScene2D PhysicsScene2D { get; private set; }

        void Start()
        {
            PhysicsScene = gameObject.scene.GetPhysicsScene();
            PhysicsScene2D = gameObject.scene.GetPhysicsScene2D();
            LocalPhysicsMode = Neutron.Server.LocalPhysicsMode;
        }

        // Update is called once per frame
        void FixedUpdate()
        {
            if (LocalPhysicsMode == LocalPhysicsMode.Physics3D)
                PhysicsScene.Simulate(Time.fixedDeltaTime);
            else if (LocalPhysicsMode == LocalPhysicsMode.Physics2D)
                PhysicsScene2D.Simulate(Time.fixedDeltaTime);
        }
    }
}