using NeutronNetwork.Helpers;
using NeutronNetwork.Internal.Packets;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Server.Internal
{
    public class PhysicsManager : MonoBehaviour
    {
        #region Event
        public static NeutronEventNoReturn<PhysicsScene> OnPhysics {
            get;
            set;
        }
        public static NeutronEventNoReturn<PhysicsScene2D> OnPhysics2D {
            get;
            set;
        }
        #endregion

        #region Properties
        public static bool IsFixedUpdate {
            get;
            set;
        }

        public bool HasPhysics {
            get => _hasPhysics;
            set => _hasPhysics = value;
        }

        public Scene Scene {
            get;
            set;
        }

        public PhysicsScene PhysicsScene {
            get;
            set;
        }

        public PhysicsScene2D PhysicsScene2D {
            get;
            set;
        }
        #endregion

        #region Fields
        [SerializeField] private bool _hasPhysics = true;
        [SerializeField] private LocalPhysicsMode _localPhysicsMode;
        #endregion

        private void Start()
        {
            _localPhysicsMode = Neutron.Server.LocalPhysicsMode;
        }

        private void Update()
        {
            if (_hasPhysics && !IsFixedUpdate)
            {
                if (_localPhysicsMode == LocalPhysicsMode.Physics3D)
                    OnPhysics?.Invoke(PhysicsScene);
                else if (_localPhysicsMode == LocalPhysicsMode.Physics2D)
                    OnPhysics2D?.Invoke(PhysicsScene2D);
                else
                    LogHelper.Error("Multiple physics scene(2D and 3D Simultaneous) not supported!");
            }
        }

        private void FixedUpdate()
        {
            if (_hasPhysics && IsFixedUpdate)
            {
                if (_localPhysicsMode == LocalPhysicsMode.Physics3D)
                    OnPhysics?.Invoke(PhysicsScene);
                else if (_localPhysicsMode == LocalPhysicsMode.Physics2D)
                    OnPhysics2D?.Invoke(PhysicsScene2D);
                else
                    LogHelper.Error("Multiple physics scene(2D and 3D Simultaneous) not supported!");
            }
        }
    }
}