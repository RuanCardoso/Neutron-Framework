using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Net.Sockets;
using System.Net;
using System.Collections.Concurrent;
using Newtonsoft.Json;

public class Utils
{
    public static bool IsServer(GameObject obj)
    {
        return obj.layer == LayerMask.NameToLayer("ServerObject");
    }

    public static int GetFreePort(ProtocolType type)
    {
        switch (type)
        {
            case ProtocolType.Udp:
                {
                    UdpClient freePort = new UdpClient(0);
                    IPEndPoint endPoint = (IPEndPoint)freePort.Client.LocalEndPoint;
                    int port = endPoint.Port;
                    freePort.Close();
                    return port;
                }
            case ProtocolType.Tcp:
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

    public static int GetUniqueID(IPEndPoint endPoint)
    {
        return Math.Abs(endPoint.GetHashCode());
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
        catch (Exception ex) { Utils.Logger(ex.Message); }
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

    public static void Enqueue(Action action, ref ConcurrentQueue<Action> cQueue)
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
    public static void LoggerError(object message)
    {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
        Debug.LogError(message);
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
}
