using System;
using UnityEngine;

namespace NeutronNetwork
{
    public class Utilities
    {
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
}
