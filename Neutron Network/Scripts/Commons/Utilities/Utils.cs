using System;
using System.Collections;
using System.Linq;
using System.Net;
using System.Net.Sockets;
using System.Threading;
using NeutronNetwork.Internal.Server;
using NeutronNetwork.Internal.Wrappers;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace NeutronNetwork
{
    public class NeutronUtils
    {
        public static int GetMaxPacketsPerSecond(float sInterval)
        {
#if UNITY_SERVER || UNITY_EDITOR
            int currentFPS = NeutronConfig.Settings.ServerSettings.FPS;
            if (sInterval == 0) return currentFPS;
            float interval = (sInterval * currentFPS);
            float MPPS = currentFPS / interval;
            return (int)MPPS;
#else
            return 0;
#endif
        }

        public static void Logger(object message)
        {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
            Debug.Log(message);
#endif
        }

        public static bool Logger(object message, object obj)
        {
#if UNITY_SERVER
        if (obj == null) { Console.WriteLine (message); return false; }
        else return true;
#else
            if (obj != null) { Debug.Log(message); return true; }
            else return false;
#endif
        }

        public static bool LoggerError(object message)
        {
#if UNITY_SERVER
            Console.WriteLine(message);
#else
            Debug.LogError(message);
#endif
            return false;
        }

        public static bool LoggerError(object message, object obj)
        {
#if UNITY_SERVER
        if (obj == null) { Console.WriteLine (message); return false; }
        else return true;
#else
            if (obj == null) { Debug.LogError(message); return false; }
            else return true;
#endif
        }

        public static void LoggerWarning(object message)
        {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
            Debug.LogWarning(message);
#endif
        }

        public static bool LoggerWarning(object message, object obj)
        {
#if UNITY_SERVER
        if (obj == null) { Console.WriteLine (message); return false; }
        else return true;
#else
            if (obj == null) { Debug.LogWarning(message); return false; }
            else return true;
#endif
        }

        public static void StackTrace(Exception ex)
        {
            var st = new System.Diagnostics.StackTrace(ex, true);
            // Get the top stack frame
            var frame = st.GetFrame(0);
            // Get the line number from the stack frame
            var line = frame.GetFileLineNumber();
            //print
            Debug.LogException(ex);
            // extra print
            LoggerError($"Exception occurred on the line: {line}, In the \"{frame.GetMethod().Name}\" method, In the \"{frame.GetMethod().DeclaringType.Name}\" class");
        }
    }
}

namespace NeutronNetwork.Internal
{
    public class InternalUtils
    {
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

        public static void CreateContainer(string containerName, bool clientOnly = false, Player ownerNetworkObjects = null, bool enablePhysics = false, GameObject[] sceneObjects = null, LocalPhysicsMode localPhysicsMode = LocalPhysicsMode.None)
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
                        }
#if UNITY_SERVER
                    if (clientOnly)
                        MonoBehaviour.Destroy(gameObject);
#endif
                    }
                }
            }
            if (enablePhysics)
            {
                GameObject simulateObject = new GameObject("Simulate");
                Simulate simulate = simulateObject.AddComponent<Simulate>();
                simulate.physicsScene = scene.GetPhysicsScene();
                MoveToContainer(simulateObject, scene.name);
            }
        }

        public static bool IsSceneObject(int networkObjectId)
        {
            return networkObjectId > 0 && networkObjectId < Neutron.GENERATE_PLAYER_ID;
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