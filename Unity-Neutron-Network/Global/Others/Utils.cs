using System.Collections;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;
using NeutronNetwork.Internal.Wrappers;
using System.Linq;

namespace NeutronNetwork.Internal
{
    public class Utils
    {
        private static int uniqueID = 3005975;
        public static int GetUniqueID(IPEndPoint endPoint) => uniqueID++;
        public static int GetFreePort(Protocol type)
        {
            switch (type)
            {
                case Protocol.Udp:
                    {
                        UdpClient freePort = new UdpClient(0);
                        IPEndPoint endPoint = (IPEndPoint)freePort.Client.LocalEndPoint;
                        int port = endPoint.Port;
                        freePort.Close();
                        return port;
                    }
                case Protocol.Tcp:
                    {
                        TcpClient freePort = new TcpClient(new IPEndPoint(IPAddress.Any, 0));
                        IPEndPoint endPoint = (IPEndPoint)freePort.Client.LocalEndPoint;
                        int port = endPoint.Port;
                        freePort.Close();
                        return port;
                    }
                default:
                    return 0;
            }
        }

        public static void Enqueue(Action action, NeutronQueue<Action> cQueue)
        {
            cQueue.SafeEnqueue(action);
        }

        public static void Dequeue(NeutronQueue<Action> cQueue, int count)
        {
            try
            {
                for (int i = 0; i < count && cQueue.Count > 0; i++)
                {
                    cQueue.SafeDequeue().Invoke();
                }
            }
            catch (Exception ex) { Utilities.StackTrace(ex); }
        }

        public static int ValidateAndDeserializeJson<T>(string json, out T obj)
        {
            obj = default;
            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    obj = JsonConvert.DeserializeObject<T>(json);
                    return 1;
                }
                catch { return 0; }
            }
            else return 2;
        }

        public static void CreateContainer(string name, bool enablePhysics = false, bool sharingObjects = false, GameObject[] sharedObjects = null, GameObject[] unsharedObjects = null, LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None)
        {
            Scene scene = SceneManager.CreateScene(name, new CreateSceneParameters(localPhysicsMode));

            if (sharedObjects != null && sharingObjects)
            {
                foreach (GameObject @object in sharedObjects)
                {
                    GameObject gameObject = MonoBehaviour.Instantiate(@object);
#if UNITY_EDITOR
                    gameObject.hideFlags = HideFlags.HideInHierarchy;
                    LinkObject linked = gameObject.AddComponent<LinkObject>();
                    linked.@object = @object;
#endif
                    MoveToContainer(gameObject, scene.name);
                    Renderer[] renderer = gameObject.GetComponentsInChildren<Renderer>();
                    if (renderer != null)
                        renderer.ToList().ForEach(x => MonoBehaviour.Destroy(x));
#if UNITY_SERVER
                MonoBehaviour.Destroy(@object);
#endif
                }
            }
#if UNITY_SERVER
        if(unsharedObjects != null)
        {
            foreach (GameObject @object in unsharedObjects)
            {
                MonoBehaviour.Destroy(@object);
            }
        }
#endif

            if (!enablePhysics || localPhysicsMode == LocalPhysicsMode.None) return;
            GameObject simulateObject = new GameObject("Simulate");
#if UNITY_EDITOR
            simulateObject.hideFlags = HideFlags.HideInHierarchy;
#endif
            MoveToContainer(simulateObject, scene.name);
            Simulate simulate = simulateObject.AddComponent<Simulate>();
            simulate.physicsScene = scene.GetPhysicsScene();
        }

        public static void MoveToContainer(GameObject obj, string name)
        {
            SceneManager.MoveGameObjectToScene(obj.transform.root.gameObject, SceneManager.GetSceneByName(name));
        }

        public static IEnumerator KeepFramerate(int framerate)
        {
            while (true)
            {
                yield return new WaitForSeconds(1f);
                QualitySettings.vSyncCount = 0;
                Application.targetFrameRate = framerate;
            }
        }
    }
}