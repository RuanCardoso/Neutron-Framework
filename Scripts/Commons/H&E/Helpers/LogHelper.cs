using System;
using UnityEngine;

namespace NeutronNetwork
{
    public static class LogHelper
    {
        public static Action<string, string, int, int> LogErrorWithoutStackTrace;
        public static void Info(object message)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.WriteLine(message);
#else
            Debug.Log(message);
#endif
        }

        public static bool Info(object message, object obj)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (obj == null)
            {
                Console.WriteLine(message);
                return false;
            }
            else
                return true;
#else
            if (obj != null)
            {
                Debug.Log(message);
                return true;
            }
            else
                return false;
#endif
        }

        public static bool Error(object message)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.WriteLine(message);
#else
            Debug.LogError(message);
#endif
            return false;
        }

        public static bool Error(object message, object obj)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (obj == null)
            {
                Console.WriteLine(message);
                return false;
            }
            else
                return true;
#else
            if (obj == null)
            {
                Debug.LogError(message);
                return false;
            }
            else
                return true;
#endif
        }

        public static void Warn(object message)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.WriteLine(message);
#else
            Debug.LogWarning(message);
#endif
        }

        public static bool Warn(object message, object obj)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            if (obj == null)
            {
                Console.WriteLine(message);
                return false;
            }
            else
                return true;
#else
            if (obj == null)
            {
                Debug.LogWarning(message);
                return false;
            }
            else
                return true;
#endif
        }

        public static void PrintInline(object message, string prefix)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.Write("\r{0}{1}", message, prefix);
#endif
        }

        public static bool ErrorWithoutStackTrace(object message)
        {
#if UNITY_SERVER && !UNITY_EDITOR
            Console.WriteLine(message);
#else
            if (LogErrorWithoutStackTrace != null)
            {
                NeutronSchedule.ScheduleTask(() =>
                {
                    LogErrorWithoutStackTrace(message.ToString(), "", 0, 0);
                });
            }
#endif
            return false;
        }

        public static void Stacktrace(Exception ex) => Debug.LogException(ex);
    }
}