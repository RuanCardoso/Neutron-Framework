using System;
using UnityEngine;

namespace NeutronNetwork
{
    public static class NeutronLogger
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

        public static void Print(string msg, LogType logType = LogType.Log)
        {
#if UNITY_SERVER
            Console.WriteLine(msg);
#else
            Debug.LogFormat(logType, LogOption.NoStacktrace, null, "{0}", msg);
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