using System;
using UnityEngine;

namespace NeutronNetwork
{
    public static class LogHelper
    {
        public static void Info(object message)
        {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
            Debug.Log(message);
#endif
        }

        public static bool Info(object message, object obj)
        {
#if UNITY_SERVER
        if (obj == null) { Console.WriteLine (message); return false; }
        else return true;
#else
            if (obj != null) { Debug.Log(message); return true; }
            else return false;
#endif
        }

        public static bool Error(object message)
        {
#if UNITY_SERVER
            Console.WriteLine(message);
#else
            Debug.LogError(message);
#endif
            return false;
        }

        public static bool Error(object message, object obj)
        {
#if UNITY_SERVER
        if (obj == null) { Console.WriteLine (message); return false; }
        else return true;
#else
            if (obj == null) { Debug.LogError(message); return false; }
            else return true;
#endif
        }

        public static void Warn(object message)
        {
#if UNITY_SERVER
        Console.WriteLine (message);
#else
            Debug.LogWarning(message);
#endif
        }

        public static bool Warn(object message, object obj)
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
            Debug.LogException(ex);
            Error($"Stacktrace: {ex.StackTrace}");
            Error($"Error: {ex.Message}");
        }
    }
}