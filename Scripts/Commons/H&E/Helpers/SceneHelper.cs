using NeutronNetwork.Internal.Packets;
using NeutronNetwork.Server.Internal;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Helpers
{
    public static class SceneHelper
    {
        public static PhysicsManager CreateContainer(string name, LocalPhysicsMode physics = LocalPhysicsMode.None)
        {
            Scene fScene = SceneManager.GetSceneByName(name);
            if (!fScene.IsValid())
            {
                Scene newScene = SceneManager.CreateScene(name, new CreateSceneParameters(physics));
                //* Cria um gerenciador de física.
                GameObject parent = new GameObject("Physics Manager");
                PhysicsManager manager = parent.AddComponent<PhysicsManager>();
                manager.Scene = newScene;
                manager.PhysicsScene = newScene.GetPhysicsScene();
                manager.PhysicsScene2D = newScene.GetPhysicsScene2D();
                //* Move o gerenciador de física para a sua cena em questão.
                MoveToContainer(parent, newScene.name);
                return manager;
            }
            else
                return null;
        }

        public static void MoveToContainer(GameObject obj, string name)
        {
            SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, SceneManager.GetSceneByName(name));
        }

        public static void MoveToContainer(GameObject obj, Scene scene)
        {
            SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, scene);
        }

        public static GameObject OnMatchmakingManager(NeutronPlayer player, bool isServer, Neutron neutron)
        {
            //* Inicializa um Matchmaking Manager e o registra na rede.
            GameObject matchManager = new GameObject("Match Manager");
            var neutronView = matchManager.AddComponent<NeutronView>();
            neutronView.AutoDestroy = false;
            //* Inicializa o iRpc Actions baseado no tipo.
            NeutronBehaviour[] actions = Neutron.Server._actions;
            if (actions.Length > 0)
            {
                #region Server Player
                NeutronPlayer owner = player;
                if (Neutron.Server._serverOwnsTheMatchManager)
                {
                    owner = PlayerHelper.MakeTheServerPlayer();
                    owner.Channel = player.Channel;
                    owner.Room = player.Room;
                    owner.Matchmaking = player.Matchmaking;
                }
                #endregion

                GameObject actionsObject = GameObject.Instantiate(actions[actions.Length - 1].gameObject, matchManager.transform);
                actionsObject.name = "Actions Object";
                foreach (Component component in actionsObject.GetComponents<Component>())
                {
                    Type type = component.GetType();
                    if (type.BaseType != typeof(NeutronBehaviour) && type != typeof(Transform))
                        GameObject.Destroy(component);
                }
                neutronView.OnNeutronRegister(owner, isServer, RegisterMode.Dynamic, neutron, short.MaxValue);
            }
            return matchManager;
        }

        public static bool IsInScene(GameObject gameObject)
        {
            return gameObject.scene.IsValid();
        }
    }
}