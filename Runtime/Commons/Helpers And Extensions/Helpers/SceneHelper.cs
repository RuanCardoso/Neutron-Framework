using NeutronNetwork.Internal.Packets;
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork.Helpers
{
    public static class SceneHelper
    {
        public static Scene CreateContainer(string name, LocalPhysicsMode physics = LocalPhysicsMode.None)
        {
            Scene fScene = SceneManager.GetSceneByName(name);
            if (!fScene.IsValid())
                return SceneManager.CreateScene(name, new CreateSceneParameters(physics));
            else
                return default;
        }

        public static void MoveToContainer(GameObject obj, string name)
        {
            Scene dstScene = SceneManager.GetSceneByName(name);
            if (dstScene.IsValid())
                SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, SceneManager.GetSceneByName(name));
            else
                LogHelper.Error($"Container {name} not found!");
        }

        public static void MoveToContainer(GameObject obj, Scene scene)
        {
            if (scene.IsValid())
                SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, scene);
            else
                LogHelper.Error("Scene is not valid!");
        }

        public static GameObject MakeMatchmakingManager(NeutronPlayer player, bool isServer, Neutron neutron)
        {
            //* Inicializa um Matchmaking Manager e o registra na rede.
            GameObject matchManager = new GameObject("Match Manager");
            matchManager.hideFlags = HideFlags.HideInHierarchy;
            var neutronView = matchManager.AddComponent<NeutronView>();
            neutronView.AutoDestroy = false;
            neutronView.Id = short.MaxValue;
            //* Inicializa o iRpc Actions baseado no tipo.
            NeutronBehaviour[] actions = Neutron.Server.Actions;

            #region Server Player
            NeutronPlayer owner = player;
            if (Neutron.Server.MatchmakingManagerOwner == OwnerMode.Server)
                owner = PlayerHelper.MakeTheServerPlayer(player.Channel, player.Room, player.Matchmaking);
            #endregion

            if (actions.Length > 0)
            {
                GameObject actionsObject = GameObject.Instantiate(actions[actions.Length - 1].gameObject, matchManager.transform);
                actionsObject.name = "Actions Object";
                foreach (Component component in actionsObject.GetComponents<Component>())
                {
                    Type type = component.GetType();
                    if (type.BaseType != typeof(NeutronBehaviour) && type.BaseType != typeof(SyncVarBehaviour) && type != typeof(Transform))
                        GameObject.Destroy(component);
                }
            }
            else { /*Continue*/ }
            neutronView.OnNeutronRegister(owner, isServer, RegisterMode.Scene, neutron);
            return matchManager;
        }

        public static bool IsInScene(GameObject gameObject)
        {
            return gameObject.scene.IsValid();
        }

        public static string GetSideTag(bool isServer)
        {
            return isServer ? "[Server-Side]" : "[Client-Side]";
        }
    }
}