using System.Collections;
using System;
using System.Net;
using System.Net.Sockets;
using UnityEngine.SceneManagement;
using UnityEngine;
using NeutronNetwork.Internal.Wrappers;
using NeutronNetwork.Internal.Server;
using System.Linq;
using System.Threading;

namespace NeutronNetwork.Internal
{
    public class Utils
    {
        public static int GetUniqueID() => ++NeutronServer.uniqueID;
        public static void Enqueue(Action action, NeutronQueue<Action> cQueue) => cQueue.SafeEnqueue(action);
        public static void ChunkDequeue(NeutronQueue<Action> cQueue, int chunkSize)
        {
            try
            {
                for (int i = 0; i < chunkSize && cQueue.SafeCount > 0; i++)
                {
                    cQueue.SafeDequeue().Invoke();
                }
            }
            catch (Exception ex) { NeutronUtils.StackTrace(ex); }
        }

        public static int GetFreePort(Protocol type)
        {
            switch (type)
            {
                case Protocol.Udp:
                    {
                        UdpClient freePort = new UdpClient(new IPEndPoint(IPAddress.Any, 0));
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

        public static void UpdateStatistics(Statistics statisticsType, int value)
        {
#if UNITY_SERVER || UNITY_EDITOR
            switch (statisticsType)
            {
                case Statistics.ClientSent:
                    {
                        int bytes = NeutronStatistics.clientBytesSent;
                        Interlocked.Exchange(ref NeutronStatistics.clientBytesSent, bytes + value);
                        break;
                    }
                case Statistics.ClientRec:
                    {
                        int bytes = NeutronStatistics.clientBytesRec;
                        Interlocked.Exchange(ref NeutronStatistics.clientBytesRec, bytes + value);
                        break;
                    }
                case Statistics.ServerSent:
                    {
                        int bytes = NeutronStatistics.serverBytesSent;
                        Interlocked.Exchange(ref NeutronStatistics.serverBytesSent, bytes + value);
                        break;
                    }
                case Statistics.ServerRec:
                    {
                        int bytes = NeutronStatistics.serverBytesRec;
                        Interlocked.Exchange(ref NeutronStatistics.serverBytesRec, bytes + value);
                        break;
                    }
            }
#endif
        }

        public static void ChangeColor(NeutronView neutronView)
        {
            Renderer renderer = neutronView.GetComponentInChildren<Renderer>();
            if (renderer != null)
                renderer.material.color = Color.red;
        }

        public static string SizeSuffixMB(int value, int decimalPlaces = 4)
        {
            double count = ((double)value / 1024) / 1024;
            count = Math.Round(count, decimalPlaces);
            return $"{count.ToString()} mB/s";
        }

        private static readonly string[] SizeSuffixes = { "B/s", "kB/s", "mB/s" };
        public static string SizeSuffix(int value, int decimalPlaces = 2)
        {
            if (decimalPlaces < 0) { throw new ArgumentOutOfRangeException("decimalPlaces"); }
            if (value < 0) { return "-" + SizeSuffix(-value, decimalPlaces); }
            if (value == 0) { return string.Format("{0:n" + decimalPlaces + "} B/s", 0); }

            // mag is 0 for bytes, 1 for KB, 2, for MB, etc.
            int mag = (int)Math.Log(value, 1024);

            // 1L << (mag * 10) == 2 ^ (10 * mag) 
            // [i.e. the number of bytes in the unit corresponding to mag]
            decimal adjustedSize = (decimal)value / (1L << (mag * 10));

            // make adjustment when the value is large enough that
            // it would round up to 1000 or more
            if (Math.Round(adjustedSize, decimalPlaces) >= 1000)
            {
                mag += 1;
                adjustedSize /= 1024;
            }

            return string.Format("{0:n" + decimalPlaces + "} {1}",
                adjustedSize,
                SizeSuffixes[mag]);
        }
    }
}