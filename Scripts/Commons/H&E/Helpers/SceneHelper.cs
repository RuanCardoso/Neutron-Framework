using NeutronNetwork;
using NeutronNetwork.Constants;
using NeutronNetwork.Internal;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class SceneHelper
{
    public static void CreateContainer(string containerName, NeutronPlayer ownerNetworkObjects = null, bool enablePhysics = false, GameObject[] sceneObjects = null, LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None)
    {
        Scene scene = SceneManager.CreateScene(containerName, new CreateSceneParameters(localPhysicsMode));
        if (sceneObjects != null)
        {
            foreach (GameObject gameObject in sceneObjects)
            {
                if (gameObject != null)
                {
                    GameObject instObject = MonoBehaviour.Instantiate(gameObject);
                    if (instObject != null)
                    {
                        MoveToContainer(instObject, scene.name);
                        var neutronViews = instObject.GetComponentsInChildren<NeutronView>();
                        if (neutronViews != null)
                        {
                            foreach (NeutronView view in neutronViews)
                            {
                                NeutronRegister.RegisterSceneObject(ownerNetworkObjects, view, true);
                            }
                        }
                        else continue;
                    }
                    else continue;
                }
                else continue;
            }
        }

        #region Physics
        if (enablePhysics)
        {
            GameObject l_SimulateObject = new GameObject("Simulate");
            NeutronSimulate l_Simulate = l_SimulateObject.AddComponent<NeutronSimulate>();
            l_Simulate.physicsScene = scene.GetPhysicsScene();
            MoveToContainer(l_SimulateObject, scene.name);
        }
        #endregion
    }

    public static bool IsSceneObject(int networkObjectId)
    {
        return networkObjectId > 0 && networkObjectId < NeutronConstants.GENERATE_PLAYER_ID;
    }

    public static void MoveToContainer(GameObject obj, string name)
    {
        SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, SceneManager.GetSceneByName(name));
    }
}