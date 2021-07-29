using NeutronNetwork;
using NeutronNetwork.Helpers;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHelper
{
    public static void CreateContainer(string name, NeutronPlayer player = null, bool hasPhysics = false, GameObject[] objects = null, LocalPhysicsMode physics = LocalPhysicsMode.None)
    {
        Scene scene = SceneManager.CreateScene(name, new CreateSceneParameters(physics));
        if (objects != null)
        {
            foreach (GameObject gameObject in objects)
            {
                if (gameObject != null)
                {
                    GameObject obj = Object.Instantiate(gameObject);
                    MoveToContainer(obj, scene.name);
                    var neutronViews = obj.GetComponentsInChildren<NeutronView>();
                    if (neutronViews.Length > 0)
                    {
                        foreach (NeutronView view in neutronViews)
                            view.OnNeutronRegister(player, true, RegisterType.Scene, null);
                    }
                    else
                        continue;
                }
                else
                    continue;
            }
        }

        if (hasPhysics)
        {
            GameObject simulateObject = new GameObject("Simulate");
            NeutronSimulate simulate = simulateObject.AddComponent<NeutronSimulate>();
            simulate.physicsScene = scene.GetPhysicsScene();
            MoveToContainer(simulateObject, scene.name);
        }
    }

    public static bool IsSceneObject(int viewID)
    {
        return viewID > 0 && viewID < Settings.GENERATE_PLAYER_ID;
    }

    public static void MoveToContainer(GameObject obj, string name)
    {
        SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, SceneManager.GetSceneByName(name));
    }
}