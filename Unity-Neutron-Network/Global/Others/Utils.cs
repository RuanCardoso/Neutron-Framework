using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using Newtonsoft.Json;
using UnityEngine.SceneManagement;

public class Utils
{
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
    static int sup = 0;
    public static int GetUniqueID(IPEndPoint endPoint)
    {
        sup++;
        Utils.Logger("Clientes conectados: " + sup);
        return Math.Abs(endPoint.GetHashCode() ^ endPoint.Port ^ new System.Random().Next(1, 999) ^ sup);
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

    public static void Dequeue(ref ConcurrentQueue<Action> cQueue, int count)
    {
        try
        {
            for (int i = 0; i < count && cQueue.Count > 0; i++)
            {
                if (cQueue.TryDequeue(out Action action))
                {
                    action.Invoke();
                }
            }
        }
        catch (Exception ex) { Utils.StackTrace(ex); }
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

    public static bool CheckIfLayersExists(out int clientLayer, out int serverLayer)
    {
        clientLayer = LayerMask.NameToLayer("ClientObject");
        serverLayer = LayerMask.NameToLayer("ServerObject");
        return clientLayer > -1 && serverLayer > -1;
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
                var renderer = gameObject.GetComponent<Renderer>();
                if (renderer != null)
                    MonoBehaviour.Destroy(renderer);
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

    public static void Enqueue(Action action, ConcurrentQueue<Action> cQueue)
    {
        cQueue.Enqueue(action);
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

    public static void LoggerError(object message)
    {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
        Debug.LogError(message);
#endif
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

        LoggerError($"Exception ocurred in: {line}");
        Debug.LogException(ex);
    }
}