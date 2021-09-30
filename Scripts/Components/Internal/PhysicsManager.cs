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
            _localPhysicsMode = Neutron.Server._localPhysicsMode;
        }

        private void OnEnable()
        {
            NeutronModule.OnUpdate += OnUpdate;
            NeutronModule.OnFixedUpdate += OnFixedUpdate;
        }

        private void OnDestroy()
        {
            NeutronModule.OnUpdate -= OnUpdate;
            NeutronModule.OnFixedUpdate -= OnFixedUpdate;
        }

        private void OnUpdate()
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

        private void OnFixedUpdate()
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

        /// <summary>
        ///* Registra todos os objetos de cena na rede.
        /// </summary>
        public void Register(NeutronPlayer player, bool isServer, Neutron neutron)
        {
            //* Inicia o registro.
            NeutronPlayer owner = player;
            if (Neutron.Server._serverOwnsTheSceneObjects)
            {
                owner = PlayerHelper.MakeTheServerPlayer();
                owner.Channel = player.Channel;
                owner.Room = player.Room;
                owner.Matchmaking = player.Matchmaking;
            }

            //* Registra todos os objetos de rede na cena.
            GameObject[] rootObjects = Scene.GetRootGameObjects();
            for (int i = 0; i < rootObjects.Length; i++)
            {
                GameObject root = rootObjects[i];
                NeutronView[] views = root.GetComponentsInChildren<NeutronView>();
                foreach (NeutronView view in views)
                {
                    if (view.This == null) // if null, not registered.
                        view.OnNeutronRegister(owner, isServer, RegisterMode.Scene, neutron);
                    else
                        continue;
                }
            }
        }
    }
}